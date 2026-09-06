using Confluent.Kafka;
using Microsoft.Extensions.Options;
using System.Text.Json;
using TNT.NotificationService.Api.Events;
using TNT.NotificationService.Api.Kafka;

namespace TNT.NotificationService.Api.BackgroundServices;

/// <summary>
/// Hosted background service that consumes UserRegistered events from Kafka.
/// Consumer group: notification-service-group
/// Topic: user-events
/// </summary>
public class UserRegisteredConsumer : BackgroundService
{
    private readonly KafkaConsumerSettings _settings;
    private readonly ILogger<UserRegisteredConsumer> _logger;

    public UserRegisteredConsumer(
        IOptions<KafkaConsumerSettings> settings,
        ILogger<UserRegisteredConsumer> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation(
                "Kafka consumer disabled (Kafka:Enabled=false). " +
                "UserRegisteredConsumer will not start.");
            return;
        }

        // Let the host finish starting before entering the synchronous Kafka poll loop.
        await Task.Yield();

        _logger.LogInformation(
            "Starting UserRegisteredConsumer. Topic={Topic}, Group={Group}, Brokers={Brokers}",
            _settings.UserEventsTopic, _settings.ConsumerGroup, _settings.BootstrapServers);

        var config = new ConsumerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            GroupId = _settings.ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
            SessionTimeoutMs = 10000
        };

        using var consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, e) =>
                _logger.LogError("Kafka consumer error: {Reason} (IsFatal={IsFatal})", e.Reason, e.IsFatal))
            .Build();

        consumer.Subscribe(_settings.UserEventsTopic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Poll with a short timeout to allow cancellation checks
                    var result = consumer.Consume(TimeSpan.FromSeconds(1));

                    if (result?.Message?.Value == null)
                        continue;

                    await ProcessMessageAsync(result.Message.Value, stoppingToken);
                }
                catch (ConsumeException ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Kafka ConsumeException. Retrying after delay.");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("UserRegisteredConsumer stopping gracefully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in UserRegisteredConsumer. Service will stop.");
            throw;
        }
        finally
        {
            consumer.Close();
            _logger.LogInformation("Kafka consumer closed.");
        }
    }

    private Task ProcessMessageAsync(string payload, CancellationToken ct)
    {
        try
        {
            var evt = JsonSerializer.Deserialize<UserRegisteredEvent>(payload,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (evt == null)
            {
                _logger.LogWarning("Received null or unparseable message from topic.");
                return Task.CompletedTask;
            }

            if (evt.EventType == "UserRegistered")
            {
                // Sprint 01: Log only. Future sprints will send actual notifications.
                _logger.LogInformation(
                    "[Notification] UserRegistered event received. " +
                    "EventId={EventId}, UserId={UserId}, Email={Email}, Role={Role}, OccurredAt={OccurredAt}",
                    evt.EventId, evt.UserId, evt.Email, evt.Role, evt.OccurredAtUtc);
            }
            else
            {
                _logger.LogWarning(
                    "Unknown event type '{EventType}' received. Skipping.", evt.EventType);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize Kafka message: {Payload}", payload);
        }

        return Task.CompletedTask;
    }
}
