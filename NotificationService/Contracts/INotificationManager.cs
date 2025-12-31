namespace NotificationService.Contracts;

public interface INotificationManager
{
    Task<ProcessMessageResult> ProcessAndSendNotificationAsync(
        NotificationMessage message,
        CancellationToken cancellationToken = default);

    Task<NotificationResult> RetryFailedNotificationAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default);
}
