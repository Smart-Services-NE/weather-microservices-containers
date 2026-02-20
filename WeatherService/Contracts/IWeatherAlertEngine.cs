namespace WeatherService.Contracts;

public interface IWeatherAlertEngine
{
    bool IsFreezing(double temperatureCelsius);
}
