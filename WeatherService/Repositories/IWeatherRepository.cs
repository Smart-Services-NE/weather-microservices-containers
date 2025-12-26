using WeatherService.Models;

namespace WeatherService.Repositories;

public interface IWeatherRepository
{
    Task<WeatherForecast?> GetForecastAsync(string zipCode);
}
