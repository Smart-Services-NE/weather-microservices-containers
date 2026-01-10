namespace NotificationService.Contracts;

public interface IKafkaProducerUtility
{
    Task PublishNotificationRecordAsync(
        NotificationRecord record,
        CancellationToken cancellationToken);
}
