namespace WeatherService.Contracts;

public enum ThresholdType
{
    TemperatureHigh,
    TemperatureLow,
    WindSpeed,
    PrecipitationProbability
}

public record SubscriptionRequest(
    string Email, 
    string ZipCode, 
    ThresholdType Type = ThresholdType.TemperatureLow, 
    double Value = 32.0, 
    string ComparisonOperator = "less-than"
);

public record SubscriptionRecord(
    Guid Id, 
    string Email, 
    string ZipCode, 
    ThresholdType Type, 
    double Value, 
    string ComparisonOperator,
    DateTime CreatedAt
);

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
