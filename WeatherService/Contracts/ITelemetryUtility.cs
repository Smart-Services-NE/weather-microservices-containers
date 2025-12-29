namespace WeatherService.Contracts;

public interface ITelemetryUtility
{
    void SetTag(string key, object? value);
    void RecordMetric(string name, double value, params KeyValuePair<string, object?>[] tags);
    IDisposable StartActivity(string operationName);
}
