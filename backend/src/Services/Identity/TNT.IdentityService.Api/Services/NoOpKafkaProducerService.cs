using TNT.IdentityService.Api.Events;
using TNT.IdentityService.Api.Interfaces;

namespace TNT.IdentityService.Api.Services;

/// <summary>
/// No-op Kafka producer for use in tests and local development without a Kafka broker.
/// Registered via DI when Kafka:Enabled is set to false in configuration.
/// </summary>
public class NoOpKafkaProducerService : IKafkaProducerService
{
    private readonly ILogger<NoOpKafkaProducerService> _logger;

    public NoOpKafkaProducerService(ILogger<NoOpKafkaProducerService> logger)
    {
        _logger = logger;
    }

    public Task PublishUserRegisteredAsync(UserRegisteredEvent evt, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[NoOp] Kafka disabled — UserRegistered event for UserId={UserId} was NOT published.",
            evt.UserId);
        return Task.CompletedTask;
    }
}
