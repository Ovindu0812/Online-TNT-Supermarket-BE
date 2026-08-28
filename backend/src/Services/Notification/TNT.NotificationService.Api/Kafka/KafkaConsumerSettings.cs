namespace TNT.NotificationService.Api.Kafka;

/// <summary>Kafka consumer configuration for the Notification Service.</summary>
public class KafkaConsumerSettings
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string UserEventsTopic { get; set; } = "user-events";
    public string ConsumerGroup { get; set; } = "notification-service-group";
    public bool Enabled { get; set; } = true;
}
