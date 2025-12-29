namespace WeatherService.Contracts;

public interface IWeatherDataAccessor
{
    Task<WeatherDataResult> GetCurrentWeatherAsync(string latitude, string longitude);
}
