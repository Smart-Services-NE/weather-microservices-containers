namespace WeatherService.Contracts;

public interface IWeatherCodeEngine
{
    string TranslateWeatherCode(int weatherCode);
    WeatherCategory GetWeatherCategory(int weatherCode);
}
