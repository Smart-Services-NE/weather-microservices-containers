namespace NotificationService.Contracts;

public interface IKafkaConsumerUtility
{
    Task<NotificationMessage?> ConsumeMessageAsync(CancellationToken cancellationToken = default);
    Task CommitOffsetAsync(CancellationToken cancellationToken = default);
    void Subscribe(IEnumerable<string> topics);
    void Close();
}
