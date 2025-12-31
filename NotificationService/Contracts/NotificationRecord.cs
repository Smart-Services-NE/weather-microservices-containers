namespace NotificationService.Contracts;

public record NotificationRecord(
    Guid Id,
    string MessageId,
    string Topic,
    string Subject,
    string Body,
    string Recipient,
    NotificationStatus Status,
    DateTime CreatedAt,
    DateTime? SentAt = null,
    int RetryCount = 0,
    string? ErrorMessage = null
);

public enum NotificationStatus
{
    Pending,
    Sent,
    Failed,
    Retrying
}
