using System.Text;
using System.Text.Json;
using NotificationService.Contracts;

namespace NotificationService.Engines;

public class NotificationEngine : INotificationEngine
{
    public NotificationMessage ParseMessage(string topic, string messageContent)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(messageContent);
            var root = jsonDoc.RootElement;

            var messageId = root.TryGetProperty("messageId", out var msgId)
                ? msgId.GetString() ?? Guid.NewGuid().ToString()
                : Guid.NewGuid().ToString();

            var subject = root.TryGetProperty("subject", out var subj)
                ? subj.GetString() ?? "Notification"
                : "Notification";

            var body = root.TryGetProperty("body", out var bdy)
                ? bdy.GetString() ?? string.Empty
                : string.Empty;

            var recipient = root.TryGetProperty("recipient", out var recip)
                ? recip.GetString() ?? string.Empty
                : string.Empty;

            var timestamp = root.TryGetProperty("timestamp", out var ts)
                ? DateTime.Parse(ts.GetString() ?? DateTime.UtcNow.ToString())
                : DateTime.UtcNow;

            Dictionary<string, string>? metadata = null;
            if (root.TryGetProperty("metadata", out var meta))
            {
                metadata = new Dictionary<string, string>();
                foreach (var prop in meta.EnumerateObject())
                {
                    metadata[prop.Name] = prop.Value.GetString() ?? string.Empty;
                }
            }

            return new NotificationMessage(
                messageId,
                topic,
                subject,
                body,
                recipient,
                timestamp,
                metadata
            );
        }
        catch (JsonException)
        {
            return new NotificationMessage(
                Guid.NewGuid().ToString(),
                topic,
                "Raw Message",
                messageContent,
                string.Empty,
                DateTime.UtcNow
            );
        }
    }

    public EmailRequest BuildEmailRequest(NotificationMessage message)
    {
        string body = message.Body;
        bool isHtml = message.Metadata?.GetValueOrDefault("isHtml")?.ToLowerInvariant() == "true";

        // Auto-generate HTML email from weather data if body is empty
        if (string.IsNullOrWhiteSpace(body) && message.WeatherData != null)
        {
            body = GenerateWeatherAlertHtml(message.WeatherData, message.Subject, message.Recipient);
            isHtml = true;
        }

        var from = message.Metadata?.GetValueOrDefault("from");
        return new EmailRequest(message.Recipient, message.Subject, body, from, isHtml);
    }

    public bool ValidateMessage(NotificationMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.Recipient))
            return false;

        if (string.IsNullOrWhiteSpace(message.Subject))
            return false;

        // Body can be empty if WeatherData is present (will be auto-generated)
        if (string.IsNullOrWhiteSpace(message.Body) && message.WeatherData == null)
            return false;

        if (!IsValidEmail(message.Recipient))
            return false;

        return true;
    }

    /// <summary>
    /// Generates a professional HTML email from weather alert data
    /// </summary>
    private string GenerateWeatherAlertHtml(WeatherAlertData weatherData, string subject, string recipient)
    {
        var severity = weatherData.Severity;
        var severityColor = severity switch
        {
            Severity.Critical => "#DC2626", // red-600
            Severity.Severe => "#EA580C",   // orange-600
            Severity.Warning => "#D97706",  // amber-600
            Severity.Info => "#2563EB",     // blue-600
            _ => "#6B7280"                  // gray-500
        };

        var alertTypeText = weatherData.AlertType switch
        {
            AlertType.SevereWeather => "Severe Weather Alert",
            AlertType.TemperatureExtreme => "Temperature Extreme Alert",
            AlertType.PrecipitationHeavy => "Heavy Precipitation Alert",
            AlertType.WindWarning => "Wind Warning",
            AlertType.StormWarning => "Storm Warning",
            AlertType.GeneralAlert => "Weather Alert",
            _ => "Weather Alert"
        };

        var location = weatherData.Location;
        var locationParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(location.City))
            locationParts.Add(location.City);
        if (!string.IsNullOrWhiteSpace(location.State))
            locationParts.Add(location.State);
        locationParts.Add(location.ZipCode);

        var locationText = string.Join(", ", locationParts);

        var weather = weatherData.WeatherConditions;
        var weatherHtml = new StringBuilder();

        if (weather.CurrentTemperature.HasValue)
        {
            var tempF = CelsiusToFahrenheit(weather.CurrentTemperature.Value);
            weatherHtml.Append($"<p><strong>Temperature:</strong> {weather.CurrentTemperature:F1}°C ({tempF:F1}°F)</p>");
        }

        if (!string.IsNullOrWhiteSpace(weather.WeatherDescription))
        {
            weatherHtml.Append($"<p><strong>Conditions:</strong> {weather.WeatherDescription}</p>");
        }

        if (weather.WindSpeed.HasValue)
        {
            var windMph = KmhToMph(weather.WindSpeed.Value);
            weatherHtml.Append($"<p><strong>Wind Speed:</strong> {weather.WindSpeed:F1} km/h ({windMph:F1} mph)</p>");
        }

        if (weather.Precipitation.HasValue && weather.Precipitation.Value > 0)
        {
            weatherHtml.Append($"<p><strong>Precipitation:</strong> {weather.Precipitation:F1} mm</p>");
        }

        return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;"">
    <div style=""background-color: {severityColor}; color: white; padding: 20px; border-radius: 8px 8px 0 0;"">
        <h1 style=""margin: 0; font-size: 24px;"">{subject}</h1>
        <p style=""margin: 5px 0 0 0; font-size: 14px; opacity: 0.9;"">{severity} - {alertTypeText}</p>
    </div>

    <div style=""background-color: #f9fafb; padding: 20px; border: 1px solid #e5e7eb; border-top: none; border-radius: 0 0 8px 8px;"">
        <h2 style=""margin-top: 0; color: #1f2937; font-size: 18px;"">Location</h2>
        <p style=""margin: 5px 0; font-size: 16px;"">{locationText}</p>

        {(weatherHtml.Length > 0 ? $@"
        <h2 style=""margin-top: 20px; color: #1f2937; font-size: 18px;"">Current Conditions</h2>
        {weatherHtml}" : "")}

        <div style=""margin-top: 30px; padding-top: 20px; border-top: 1px solid #d1d5db; font-size: 12px; color: #6b7280;"">
            <p>This is an automated weather alert from Weather App.</p>
            <p>Sent to: {recipient}</p>
        </div>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Converts Celsius to Fahrenheit
    /// </summary>
    private static double CelsiusToFahrenheit(double celsius) => (celsius * 9 / 5) + 32;

    /// <summary>
    /// Converts km/h to mph
    /// </summary>
    private static double KmhToMph(double kmh) => kmh * 0.621371;

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
