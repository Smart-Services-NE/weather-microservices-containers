namespace WeatherWeb.Models;

public record WeatherForecast(
    string City, 
    string State, 
    string ZipCode, 
    int TemperatureF, 
    string Summary, 
    string Date,
    IEnumerable<HourlyForecast>? HourlyForecasts = null,
    IEnumerable<DailyForecast>? DailyForecasts = null);
