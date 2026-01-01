namespace NotificationService.Contracts;

/// <summary>
/// Type of weather alert
/// </summary>
public enum AlertType
{
    SevereWeather,
    TemperatureExtreme,
    PrecipitationHeavy,
    WindWarning,
    StormWarning,
    GeneralAlert
}

/// <summary>
/// Severity level of the alert
/// </summary>
public enum Severity
{
    Info,
    Warning,
    Severe,
    Critical
}

/// <summary>
/// Location information for the weather alert
/// </summary>
/// <param name="ZipCode">ZIP code of the location</param>
/// <param name="City">City name (optional)</param>
/// <param name="State">State name (optional)</param>
/// <param name="Latitude">Latitude coordinate (optional)</param>
/// <param name="Longitude">Longitude coordinate (optional)</param>
public record LocationData(
    string ZipCode,
    string? City = null,
    string? State = null,
    double? Latitude = null,
    double? Longitude = null
);

/// <summary>
/// Current weather conditions data
/// </summary>
/// <param name="CurrentTemperature">Temperature in Celsius (optional)</param>
/// <param name="WeatherCode">WMO weather code (optional)</param>
/// <param name="WeatherDescription">Human-readable weather description (optional)</param>
/// <param name="WindSpeed">Wind speed in km/h (optional)</param>
/// <param name="Precipitation">Precipitation amount in mm (optional)</param>
public record WeatherConditionsData(
    double? CurrentTemperature = null,
    int? WeatherCode = null,
    string? WeatherDescription = null,
    double? WindSpeed = null,
    double? Precipitation = null
);

/// <summary>
/// Complete weather alert data
/// </summary>
/// <param name="AlertType">Type of weather alert</param>
/// <param name="Severity">Severity level</param>
/// <param name="Location">Location information</param>
/// <param name="WeatherConditions">Current weather conditions</param>
public record WeatherAlertData(
    AlertType AlertType,
    Severity Severity,
    LocationData Location,
    WeatherConditionsData WeatherConditions
);
