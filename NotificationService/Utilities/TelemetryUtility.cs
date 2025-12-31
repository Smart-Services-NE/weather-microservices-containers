using System.Diagnostics;
using NotificationService.Contracts;

namespace NotificationService.Utilities;

public class TelemetryUtility : ITelemetryUtility
{
    private static readonly ActivitySource ActivitySource = new("NotificationService");

    public void SetTag(string key, object? value)
    {
        Activity.Current?.SetTag(key, value);
    }

    public void RecordMetric(string name, double value)
    {
        Activity.Current?.AddEvent(new ActivityEvent(name,
            tags: new ActivityTagsCollection { { "value", value } }));
    }

    public IDisposable? StartActivity(string operationName)
    {
        return ActivitySource.StartActivity(operationName);
    }
}
