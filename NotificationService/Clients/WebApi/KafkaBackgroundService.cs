using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Contracts;

namespace NotificationService.Api;

public class KafkaBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KafkaBackgroundService> _logger;
    private readonly string[] _topics = { "weather-alerts", "general-events" };

    public KafkaBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<KafkaBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Kafka Background Service starting...");

        var kafkaConsumer = _serviceProvider.GetRequiredService<IKafkaConsumerUtility>();

        kafkaConsumer.Subscribe(_topics);

        _logger.LogInformation("Subscribed to topics: {Topics}", string.Join(", ", _topics));

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var message = await kafkaConsumer.ConsumeMessageAsync(stoppingToken);

                    if (message == null)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(100), stoppingToken);
                        continue;
                    }

                    _logger.LogInformation(
                        "Received message from topic {Topic}: MessageId={MessageId}",
                        message.Topic,
                        message.MessageId);

                    using var processingScope = _serviceProvider.CreateScope();
                    var notificationManager = processingScope.ServiceProvider
                        .GetRequiredService<INotificationManager>();

                    var result = await notificationManager.ProcessAndSendNotificationAsync(
                        message,
                        stoppingToken);

                    if (result.Success)
                    {
                        await kafkaConsumer.CommitOffsetAsync(stoppingToken);

                        _logger.LogInformation(
                            "Successfully processed and sent notification for MessageId={MessageId}",
                            message.MessageId);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Failed to process notification for MessageId={MessageId}: {Error}",
                            message.MessageId,
                            result.Error?.Message);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Kafka consumer operation cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing Kafka message");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }
        finally
        {
            _logger.LogInformation("Closing Kafka consumer...");
            kafkaConsumer.Close();
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Kafka Background Service stopping...");
        await base.StopAsync(cancellationToken);
    }
}
