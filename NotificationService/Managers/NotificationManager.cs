using Microsoft.Extensions.Logging;
using NotificationService.Contracts;

namespace NotificationService.Managers;

public class NotificationManager : INotificationManager
{
    private readonly INotificationEngine _notificationEngine;
    private readonly IEmailAccessor _emailAccessor;
    private readonly INotificationStorageAccessor _storageAccessor;
    private readonly IRetryPolicyUtility _retryPolicyUtility;
    private readonly ITelemetryUtility _telemetryUtility;
    private readonly IKafkaProducerUtility _kafkaProducerUtility;
    private readonly ILogger<NotificationManager> _logger;

    public NotificationManager(
        INotificationEngine notificationEngine,
        IEmailAccessor emailAccessor,
        INotificationStorageAccessor storageAccessor,
        IRetryPolicyUtility retryPolicyUtility,
        ITelemetryUtility telemetryUtility,
        IKafkaProducerUtility kafkaProducerUtility,
        ILogger<NotificationManager> logger)
    {
        _notificationEngine = notificationEngine;
        _emailAccessor = emailAccessor;
        _storageAccessor = storageAccessor;
        _retryPolicyUtility = retryPolicyUtility;
        _telemetryUtility = telemetryUtility;
        _kafkaProducerUtility = kafkaProducerUtility;
        _logger = logger;
    }

    public async Task<ProcessMessageResult> ProcessAndSendNotificationAsync(
        NotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        using var activity = _telemetryUtility.StartActivity("ProcessAndSendNotification");

        try
        {
            _telemetryUtility.SetTag("topic", message.Topic);
            _telemetryUtility.SetTag("message.id", message.MessageId);
            _telemetryUtility.SetTag("message.recipient", message.Recipient);

            if (!_notificationEngine.ValidateMessage(message))
            {
                _logger.LogWarning(
                    "Invalid message received: MessageId={MessageId}, Topic={Topic}",
                    message.MessageId,
                    message.Topic);

                return new ProcessMessageResult(
                    Success: false,
                    Error: new ErrorInfo("INVALID_MESSAGE", "Message validation failed")
                );
            }

            var existingRecord = await _storageAccessor.GetByMessageIdAsync(
                message.MessageId,
                cancellationToken);

            if (existingRecord != null)
            {
                _logger.LogInformation(
                    "Duplicate message detected: MessageId={MessageId}",
                    message.MessageId);

                return new ProcessMessageResult(
                    Success: true,
                    Record: existingRecord
                );
            }

            var record = new NotificationRecord(
                Id: Guid.NewGuid(),
                MessageId: message.MessageId,
                Topic: message.Topic,
                Subject: message.Subject,
                Body: message.Body,
                Recipient: message.Recipient,
                Status: NotificationStatus.Pending,
                CreatedAt: DateTime.UtcNow
            );

            record = await _storageAccessor.CreateAsync(record, cancellationToken);

            var emailRequest = _notificationEngine.BuildEmailRequest(message);

            NotificationResult emailResult;
            try
            {
                emailResult = await _retryPolicyUtility.ExecuteWithRetryAsync(
                    async ct => await _emailAccessor.SendEmailAsync(emailRequest, ct),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email after retries: MessageId={MessageId}", message.MessageId);

                var failedRecord = record with
                {
                    Status = NotificationStatus.Failed,
                    RetryCount = 5,
                    ErrorMessage = ex.Message
                };

                failedRecord = await _storageAccessor.UpdateAsync(failedRecord, cancellationToken);

                _telemetryUtility.RecordMetric("notification.failed", 1);

                return new ProcessMessageResult(
                    Success: false,
                    Record: failedRecord,
                    Error: new ErrorInfo("SEND_FAILED", $"Failed to send email: {ex.Message}")
                );
            }

            if (emailResult.Success)
            {
                var sentRecord = record with
                {
                    Status = NotificationStatus.Sent,
                    SentAt = DateTime.UtcNow
                };

                sentRecord = await _storageAccessor.UpdateAsync(sentRecord, cancellationToken);

                // Publish to Kafka for Cosmos DB sync (fire-and-forget)
                try
                {
                    await _kafkaProducerUtility.PublishNotificationRecordAsync(sentRecord, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to publish notification record to Kafka: MessageId={MessageId}", message.MessageId);
                }

                _telemetryUtility.RecordMetric("notification.sent", 1);

                _logger.LogInformation(
                    "Notification sent successfully: MessageId={MessageId}, Recipient={Recipient}",
                    message.MessageId,
                    message.Recipient);

                return new ProcessMessageResult(
                    Success: true,
                    Record: sentRecord
                );
            }
            else
            {
                var failedRecord = record with
                {
                    Status = NotificationStatus.Failed,
                    ErrorMessage = emailResult.Error?.Message
                };

                failedRecord = await _storageAccessor.UpdateAsync(failedRecord, cancellationToken);

                // Publish failed record to Kafka
                try
                {
                    await _kafkaProducerUtility.PublishNotificationRecordAsync(failedRecord, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to publish failed notification record to Kafka");
                }

                _telemetryUtility.RecordMetric("notification.failed", 1);

                return new ProcessMessageResult(
                    Success: false,
                    Record: failedRecord,
                    Error: emailResult.Error
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing notification");

            _telemetryUtility.RecordMetric("notification.error", 1);

            return new ProcessMessageResult(
                Success: false,
                Error: new ErrorInfo("UNEXPECTED_ERROR", ex.Message)
            );
        }
    }

    public async Task<NotificationResult> RetryFailedNotificationAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        using var activity = _telemetryUtility.StartActivity("RetryFailedNotification");

        try
        {
            var record = await _storageAccessor.GetByIdAsync(notificationId, cancellationToken);

            if (record == null)
            {
                return new NotificationResult(
                    Success: false,
                    Error: new ErrorInfo("NOT_FOUND", "Notification record not found")
                );
            }

            if (record.Status == NotificationStatus.Sent)
            {
                return new NotificationResult(
                    Success: true,
                    MessageId: record.MessageId
                );
            }

            var retryingRecord = record with
            {
                Status = NotificationStatus.Retrying,
                RetryCount = record.RetryCount + 1
            };

            await _storageAccessor.UpdateAsync(retryingRecord, cancellationToken);

            var emailRequest = new EmailRequest(
                retryingRecord.Recipient,
                retryingRecord.Subject,
                retryingRecord.Body
            );

            var emailResult = await _emailAccessor.SendEmailAsync(emailRequest, cancellationToken);

            if (emailResult.Success)
            {
                var sentRecord = retryingRecord with
                {
                    Status = NotificationStatus.Sent,
                    SentAt = DateTime.UtcNow
                };

                await _storageAccessor.UpdateAsync(sentRecord, cancellationToken);

                // Publish retried record
                try
                {
                    await _kafkaProducerUtility.PublishNotificationRecordAsync(sentRecord, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to publish retried notification to Kafka");
                }

                _telemetryUtility.RecordMetric("notification.retry.success", 1);

                return new NotificationResult(
                    Success: true,
                    MessageId: sentRecord.MessageId
                );
            }
            else
            {
                var failedRecord = retryingRecord with
                {
                    Status = NotificationStatus.Failed,
                    ErrorMessage = emailResult.Error?.Message
                };

                await _storageAccessor.UpdateAsync(failedRecord, cancellationToken);

                // Publish failed retry record
                try
                {
                    await _kafkaProducerUtility.PublishNotificationRecordAsync(failedRecord, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to publish failed retry to Kafka");
                }

                _telemetryUtility.RecordMetric("notification.retry.failed", 1);

                return emailResult;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying notification {NotificationId}", notificationId);

            return new NotificationResult(
                Success: false,
                Error: new ErrorInfo("RETRY_ERROR", ex.Message)
            );
        }
    }
}
