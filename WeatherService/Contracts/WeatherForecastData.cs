namespace WeatherService.Contracts;

public record WeatherForecastData(
    string City,
    string State,
    string ZipCode,
    int TemperatureF,
    string Summary,
    string Date,
    GeoLocation Location,
    IEnumerable<HourlyForecast>? HourlyForecasts = null,
    IEnumerable<DailyForecast>? DailyForecasts = null
);
