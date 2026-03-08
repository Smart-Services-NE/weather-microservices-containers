using System.Text.Json.Serialization;

namespace WeatherService.Contracts;

/// <summary>
/// Simple C# DTO for weather alert messages from Kafka
/// </summary>
public class WeatherAlertDto
{
    [JsonPropertyName("messageId")]
    public string MessageId { get; set; } = string.Empty;

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    [JsonPropertyName("recipient")]
    public string Recipient { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("alertType")]
    public string? AlertType { get; set; }

    [JsonPropertyName("severity")]
    public string? Severity { get; set; }

    [JsonPropertyName("location")]
    public LocationDto? Location { get; set; }

    [JsonPropertyName("weatherConditions")]
    public WeatherConditionsDto? WeatherConditions { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
}

public class LocationDto
{
    [JsonPropertyName("zipCode")]
    public string? ZipCode { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }
}

public class WeatherConditionsDto
{
    [JsonPropertyName("currentTemperature")]
    public double? CurrentTemperature { get; set; }

    [JsonPropertyName("weatherCode")]
    public int? WeatherCode { get; set; }

    [JsonPropertyName("weatherDescription")]
    public string? WeatherDescription { get; set; }

    [JsonPropertyName("windSpeed")]
    public double? WindSpeed { get; set; }

    [JsonPropertyName("precipitation")]
    public double? Precipitation { get; set; }
}
