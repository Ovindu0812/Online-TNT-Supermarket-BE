using TNT.IdentityService.Api.Events;
using TNT.IdentityService.Api.Interfaces;

namespace TNT.IdentityService.Tests;

/// <summary>
/// Fake Kafka producer for integration tests.
/// Captures published events in memory so tests can assert on them.
/// Does not require a running Kafka broker.
/// </summary>
public class FakeKafkaProducerService : IKafkaProducerService
{
    public List<UserRegisteredEvent> PublishedEvents { get; } = new();

    public Task PublishUserRegisteredAsync(UserRegisteredEvent evt, CancellationToken ct = default)
    {
        PublishedEvents.Add(evt);
        return Task.CompletedTask;
    }
}
