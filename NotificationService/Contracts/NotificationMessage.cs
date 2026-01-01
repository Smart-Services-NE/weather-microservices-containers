namespace NotificationService.Contracts;

public record NotificationMessage(
    string MessageId,
    string Topic,
    string Subject,
    string Body,
    string Recipient,
    DateTime Timestamp,
    Dictionary<string, string>? Metadata = null,
    WeatherAlertData? WeatherData = null
);
