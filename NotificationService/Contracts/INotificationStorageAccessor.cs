namespace NotificationService.Contracts;

public interface INotificationStorageAccessor
{
    Task<NotificationRecord> CreateAsync(NotificationRecord record, CancellationToken cancellationToken = default);
    Task<NotificationRecord> UpdateAsync(NotificationRecord record, CancellationToken cancellationToken = default);
    Task<NotificationRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<NotificationRecord?> GetByMessageIdAsync(string messageId, CancellationToken cancellationToken = default);
    Task<IEnumerable<NotificationRecord>> GetPendingRetriesAsync(CancellationToken cancellationToken = default);
}
