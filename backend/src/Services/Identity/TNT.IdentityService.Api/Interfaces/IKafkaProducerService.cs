using TNT.IdentityService.Api.Events;

namespace TNT.IdentityService.Api.Interfaces;

/// <summary>
/// Kafka producer service for publishing domain events.
/// </summary>
public interface IKafkaProducerService
{
    /// <summary>
    /// Publishes a UserRegistered event to the configured topic.
    /// Failures are logged but do not propagate to callers (fire-and-forget).
    /// </summary>
    Task PublishUserRegisteredAsync(UserRegisteredEvent evt, CancellationToken ct = default);
}
