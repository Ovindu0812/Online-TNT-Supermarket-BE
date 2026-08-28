using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TNT.IdentityService.Api.Data;
using TNT.IdentityService.Api.Interfaces;

namespace TNT.IdentityService.Tests;

/// <summary>
/// Custom WebApplicationFactory for integration tests.
/// - Replaces IdentityDbContext with an in-memory database (unique per instance).
/// - Replaces IKafkaProducerService with a fake/no-op implementation.
/// - Loads appsettings.Testing.json so JWT is configured with a known test secret.
/// </summary>
public class IdentityWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Use Testing environment so appsettings.Testing.json is loaded
        builder.UseEnvironment("Testing");

        // Add appsettings.Testing.json from the Identity project output directory
        builder.ConfigureAppConfiguration((ctx, config) =>
        {
            config.AddJsonFile(
                Path.Combine(AppContext.BaseDirectory, "appsettings.Testing.json"),
                optional: false,
                reloadOnChange: false);
        });

        builder.ConfigureServices(services =>
        {
            // ── Remove the real DbContext registration ──────────────────────
            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<IdentityDbContext>));
            if (dbDescriptor != null) services.Remove(dbDescriptor);

            // ── Register an isolated in-memory database ─────────────────────
            // A unique DB name per factory ensures test isolation.
            var dbName = $"IdentityTestDb_{Guid.NewGuid():N}";
            services.AddDbContext<IdentityDbContext>(opts =>
                opts.UseInMemoryDatabase(dbName));

            // ── Remove the real Kafka producer ──────────────────────────────
            var kafkaDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IKafkaProducerService));
            if (kafkaDescriptor != null) services.Remove(kafkaDescriptor);

            // ── Register the fake (capturable) Kafka producer ───────────────
            services.AddSingleton<IKafkaProducerService, FakeKafkaProducerService>();
        });
    }
}
