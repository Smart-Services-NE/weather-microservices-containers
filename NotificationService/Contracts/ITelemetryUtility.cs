namespace NotificationService.Contracts;

public interface ITelemetryUtility
{
    void SetTag(string key, object? value);
    void RecordMetric(string name, double value);
    IDisposable? StartActivity(string operationName);
}
