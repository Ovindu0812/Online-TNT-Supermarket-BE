namespace TNT.IdentityService.Api.Events;

/// <summary>
/// Domain event published to Kafka when a new user registers.
/// Topic: user-events
/// EventType: UserRegistered
/// </summary>
public class UserRegisteredEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = "UserRegistered";
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
