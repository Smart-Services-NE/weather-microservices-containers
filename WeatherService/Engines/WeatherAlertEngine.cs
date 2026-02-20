using WeatherService.Contracts;

namespace WeatherService.Engines;

public class WeatherAlertEngine : IWeatherAlertEngine
{
    public bool IsFreezing(double temperatureCelsius)
    {
        return temperatureCelsius <= 0;
    }
}
