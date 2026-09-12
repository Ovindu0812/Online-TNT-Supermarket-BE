using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .Enrich.FromLogContext()
           .WriteTo.Console());

    // YARP Reverse Proxy — reads routes and clusters from appsettings.json
    builder.Services.AddReverseProxy()
        .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            var allowedOrigins = builder.Configuration.GetValue<string>("Cors:AllowedOrigin") ?? "http://localhost:5173";
            var origins = allowedOrigins.Split(",", StringSplitOptions.RemoveEmptyEntries)
                .Select(origin => origin.Trim())
                .ToArray();
            policy
                .WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    builder.Services.AddHealthChecks();

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseCors("AllowFrontend");

    // Gateway passes all requests through to downstream services
    app.MapReverseProxy();

    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "TNT.ApiGateway failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
