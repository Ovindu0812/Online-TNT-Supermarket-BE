using Confluent.Kafka;
using Microsoft.Extensions.Options;
using System.Text.Json;
using TNT.IdentityService.Api.Events;
using TNT.IdentityService.Api.Interfaces;
using TNT.IdentityService.Api.Kafka;

namespace TNT.IdentityService.Api.Services;

/// <summary>
/// Kafka producer service that publishes domain events.
/// Failures are logged but not propagated — Kafka unavailability must not break registration.
/// </summary>
public class KafkaProducerService : IKafkaProducerService, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly KafkaSettings _settings;
    private readonly ILogger<KafkaProducerService> _logger;
    private bool _disposed;

    public KafkaProducerService(IOptions<KafkaSettings> settings, ILogger<KafkaProducerService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            ClientId = _settings.ClientId,
            // Reasonable defaults for development
            MessageTimeoutMs = 5000,
            RequestTimeoutMs = 3000
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishUserRegisteredAsync(UserRegisteredEvent evt, CancellationToken ct = default)
    {
        try
        {
            var payload = JsonSerializer.Serialize(evt);
            var message = new Message<string, string>
            {
                Key = evt.UserId.ToString(),
                Value = payload
            };

            var result = await _producer.ProduceAsync(_settings.UserRegisteredTopic, message, ct);
            _logger.LogInformation(
                "UserRegistered event published. UserId={UserId}, Offset={Offset}, Topic={Topic}",
                evt.UserId, result.Offset, result.Topic);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Kafka publish cancelled for UserId={UserId}", evt.UserId);
        }
        catch (ProduceException<string, string> ex)
        {
            // Log but do NOT throw — Kafka unavailability must not break registration
            _logger.LogError(ex,
                "Kafka ProduceException when publishing UserRegistered for UserId={UserId}. Event will not be retried in this sprint.",
                evt.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error publishing UserRegistered for UserId={UserId}",
                evt.UserId);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _producer.Flush(TimeSpan.FromSeconds(5));
            _producer.Dispose();
            _disposed = true;
        }
    }
}
