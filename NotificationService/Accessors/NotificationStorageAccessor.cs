using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NotificationService.Contracts;

namespace NotificationService.Accessors;

public class NotificationStorageAccessor : INotificationStorageAccessor
{
    private readonly NotificationDbContext _dbContext;
    private readonly ILogger<NotificationStorageAccessor> _logger;

    public NotificationStorageAccessor(
        NotificationDbContext dbContext,
        ILogger<NotificationStorageAccessor> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<NotificationRecord> CreateAsync(
        NotificationRecord record,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _dbContext.Notifications.Add(record);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Created notification record with ID {Id} for message {MessageId}",
                record.Id,
                record.MessageId);

            return record;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error creating notification record");
            throw;
        }
    }

    public async Task<NotificationRecord> UpdateAsync(
        NotificationRecord record,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _dbContext.Notifications.Update(record);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Updated notification record with ID {Id}",
                record.Id);

            return record;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error updating notification record");
            throw;
        }
    }

    public async Task<NotificationRecord?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task<NotificationRecord?> GetByMessageIdAsync(
        string messageId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.MessageId == messageId, cancellationToken);
    }

    public async Task<IEnumerable<NotificationRecord>> GetPendingRetriesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notifications
            .Where(n => n.Status == NotificationStatus.Retrying || n.Status == NotificationStatus.Failed)
            .OrderBy(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
