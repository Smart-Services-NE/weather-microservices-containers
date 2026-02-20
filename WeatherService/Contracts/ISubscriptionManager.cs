namespace WeatherService.Contracts;

public record SubscriptionRequest(string Email, string ZipCode);

public record SubscriptionRecord(Guid Id, string Email, string ZipCode, DateTime CreatedAt);

public interface ISubscriptionAccessor
{
    Task<Result> CreateSubscriptionAsync(SubscriptionRecord record, CancellationToken ct);
    Task<IEnumerable<SubscriptionRecord>> GetAllSubscriptionsAsync(CancellationToken ct);
    Task<Result> DeleteSubscriptionAsync(string email, string zipCode, CancellationToken ct);
}

public interface ISubscriptionManager
{
    Task<Result> SubscribeAsync(SubscriptionRequest request, CancellationToken ct);
    Task<Result> ProcessSubscriptionsAsync(CancellationToken ct);
}
