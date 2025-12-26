namespace WeatherService.Models;

public record WeatherForecast(
    string City, 
    string State, 
    string ZipCode, 
    int TemperatureF, 
    string Summary, 
    string Date);
