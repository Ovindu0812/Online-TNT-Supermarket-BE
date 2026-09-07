namespace TNT.IdentityService.Api.Kafka;

/// <summary>Kafka configuration settings bound from appsettings.json.</summary>
public class KafkaSettings
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string UserRegisteredTopic { get; set; } = "user-events";
    public string ClientId { get; set; } = "identity-service";
}
