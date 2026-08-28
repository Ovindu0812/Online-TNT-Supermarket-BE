using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using TNT.IdentityService.Api.Data;
using TNT.IdentityService.Api.DTOs;
using TNT.IdentityService.Api.Interfaces;
using TNT.IdentityService.Api.Kafka;
using TNT.IdentityService.Api.Middleware;
using TNT.IdentityService.Api.Services;

// ──────────────────────────────────────────────────────────────────────────────
// Serilog bootstrap logger (captures startup errors before host is built)
// ──────────────────────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting TNT.IdentityService.Api");

    var builder = WebApplication.CreateBuilder(args);

    // ──────────────────────────────────────────────────────────────────────────
    // Serilog
    // ──────────────────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, services, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .ReadFrom.Services(services)
           .Enrich.FromLogContext()
           .WriteTo.Console());

    // ──────────────────────────────────────────────────────────────────────────
    // Configuration binding
    // ──────────────────────────────────────────────────────────────────────────
    builder.Services.Configure<JwtSettings>(
        builder.Configuration.GetSection(JwtSettings.SectionName));
    builder.Services.Configure<KafkaSettings>(
        builder.Configuration.GetSection(KafkaSettings.SectionName));

    // ──────────────────────────────────────────────────────────────────────────
    // Database — PostgreSQL via EF Core
    // ──────────────────────────────────────────────────────────────────────────
    builder.Services.AddDbContext<IdentityDbContext>(opts =>
        opts.UseNpgsql(builder.Configuration.GetConnectionString("IdentityDb")));

    // ──────────────────────────────────────────────────────────────────────────
    // JWT Authentication
    // ──────────────────────────────────────────────────────────────────────────
    var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
        ?? throw new InvalidOperationException("Jwt configuration section is missing.");

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opts =>
        {
            opts.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                ClockSkew = TimeSpan.Zero
            };
        });

    builder.Services.AddAuthorization();

    // ──────────────────────────────────────────────────────────────────────────
    // Application services
    // ──────────────────────────────────────────────────────────────────────────
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<ITokenService, TokenService>();

    // Kafka: register real or no-op producer based on configuration
    var kafkaEnabled = builder.Configuration.GetValue<bool>("Kafka:Enabled", defaultValue: false);
    if (kafkaEnabled)
    {
        builder.Services.AddSingleton<IKafkaProducerService, KafkaProducerService>();
    }
    else
    {
        builder.Services.AddSingleton<IKafkaProducerService, NoOpKafkaProducerService>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // FluentValidation
    // ──────────────────────────────────────────────────────────────────────────
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

    // ──────────────────────────────────────────────────────────────────────────
    // Controllers
    // ──────────────────────────────────────────────────────────────────────────
    builder.Services.AddControllers();

    // ──────────────────────────────────────────────────────────────────────────
    // Swagger / OpenAPI
    // ──────────────────────────────────────────────────────────────────────────
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "TNT Identity Service",
            Version = "v1",
            Description = "Authentication and identity management for TNT Supermarket"
        });

        // JWT Bearer security definition
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter your JWT token. Example: Bearer {your-token}"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });

        // Include XML comments if available
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }
    });

    // ──────────────────────────────────────────────────────────────────────────
    // Health checks
    // ──────────────────────────────────────────────────────────────────────────
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<IdentityDbContext>(name: "identity-db");

    // ──────────────────────────────────────────────────────────────────────────
    // CORS (allow frontend during local dev)
    // ──────────────────────────────────────────────────────────────────────────
    builder.Services.AddCors(opts =>
    {
        opts.AddPolicy("AllowFrontend", policy =>
            policy
                .WithOrigins(
                    builder.Configuration.GetValue<string>("Cors:AllowedOrigin") ?? "http://localhost:5173")
                .AllowAnyHeader()
                .AllowAnyMethod());
    });

    // ──────────────────────────────────────────────────────────────────────────
    // Build app
    // ──────────────────────────────────────────────────────────────────────────
    var app = builder.Build();

    // Apply pending migrations on startup in Development (skipped when using InMemory in tests)
    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        if (db.Database.IsRelational())
        {
            db.Database.Migrate();
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Middleware pipeline
    // ──────────────────────────────────────────────────────────────────────────
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TNT Identity Service v1");
        c.RoutePrefix = string.Empty; // Serve swagger at root
    });

    app.UseSerilogRequestLogging();
    app.UseCors("AllowFrontend");
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "TNT.IdentityService.Api failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Make Program accessible for WebApplicationFactory in integration tests
public partial class Program { }
