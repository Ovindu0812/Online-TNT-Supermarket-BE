using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TNT.IdentityService.Api.Data;
using TNT.IdentityService.Api.Interfaces;

namespace TNT.IdentityService.Tests;

/// <summary>
/// Custom WebApplicationFactory for Identity Service integration tests.
/// Provides:
/// - InMemory EF Core database (unique per factory instance — no shared state between test classes)
/// - Fake Kafka producer (no broker required)
/// - Fully configured JWT settings via in-memory config (no file dependency)
/// </summary>
public class IdentityWebApplicationFactory : WebApplicationFactory<Program>
{
    // Shared known test secret — same key used in both JWT generation and validation
    public const string TestJwtSecretKey = "test-secret-key-for-identity-tests-at-least-32-chars!";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Inject all required settings directly — no appsettings file dependency
        builder.ConfigureAppConfiguration((ctx, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // JWT — must be consistent with what Program.cs configures
                ["Jwt:Issuer"]             = "TNT.IdentityService",
                ["Jwt:Audience"]           = "TNT.Supermarket",
                ["Jwt:SecretKey"]          = TestJwtSecretKey,
                ["Jwt:AccessTokenMinutes"] = "15",
                ["Jwt:RefreshTokenDays"]   = "7",

                // Kafka — disabled; FakeKafkaProducerService is registered below
                ["Kafka:Enabled"]              = "false",
                ["Kafka:BootstrapServers"]     = "localhost:9092",
                ["Kafka:UserRegisteredTopic"]  = "user-events",
                ["Kafka:ClientId"]             = "identity-service-test",

                // CORS — not exercised in tests but avoids config errors
                ["Cors:AllowedOrigin"] = "http://localhost:5173",

                // ConnectionString — overridden by InMemory DB below, but must exist
                ["ConnectionStrings:IdentityDb"] = "InMemory",
            });
        });

        builder.ConfigureServices(services =>
        {
            // ── Remove the real Postgres DbContext ──────────────────────────
            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<IdentityDbContext>));
            if (dbDescriptor != null) services.Remove(dbDescriptor);

            // ── Use a unique in-memory database per factory instance ─────────
            var dbName = $"IdentityTestDb_{Guid.NewGuid():N}";
            services.AddDbContext<IdentityDbContext>(opts =>
                opts.UseInMemoryDatabase(dbName));

            // ── Remove real Kafka producer, inject fake one ──────────────────
            var kafkaDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IKafkaProducerService));
            if (kafkaDescriptor != null) services.Remove(kafkaDescriptor);

            services.AddSingleton<IKafkaProducerService, FakeKafkaProducerService>();
        });
    }
}
