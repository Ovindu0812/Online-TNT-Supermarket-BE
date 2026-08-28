namespace TNT.NotificationService.Api.Events;

/// <summary>
/// Shape of the UserRegistered event consumed from the user-events Kafka topic.
/// Matches the event published by the Identity Service.
/// </summary>
public class UserRegisteredEvent
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
