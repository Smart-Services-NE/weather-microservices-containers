using System.Diagnostics;
using WeatherService.Contracts;

namespace WeatherService.Utilities;

public class TelemetryUtility : ITelemetryUtility
{
    private readonly ActivitySource _activitySource;

    public TelemetryUtility()
    {
        _activitySource = new ActivitySource("WeatherService");
    }

    public void SetTag(string key, object? value)
    {
        Activity.Current?.SetTag(key, value);
    }

    public void RecordMetric(string name, double value, params KeyValuePair<string, object?>[] tags)
    {
        Activity.Current?.AddEvent(new ActivityEvent(name, tags: new ActivityTagsCollection(tags)));
    }

    public IDisposable StartActivity(string operationName)
    {
        Activity? activity = _activitySource.StartActivity(operationName);
        if (activity != null)
            return activity;

        return new NullActivity();
    }

    private class NullActivity : IDisposable
    {
        public void Dispose() { }
    }
}
