using WeatherService.Contracts;

namespace WeatherService.Engines;

public class WeatherCodeEngine : IWeatherCodeEngine
{
    public string TranslateWeatherCode(int weatherCode) => weatherCode switch
    {
        0 => "Clear sky",
        1 or 2 or 3 => "Mainly clear, partly cloudy, and overcast",
        45 or 48 => "Fog",
        51 or 53 or 55 => "Drizzle",
        61 or 63 or 65 => "Rain",
        71 or 73 or 75 => "Snow fall",
        77 => "Snow grains",
        80 or 81 or 82 => "Rain showers",
        85 or 86 => "Snow showers",
        95 => "Thunderstorm",
        96 or 99 => "Thunderstorm with slight and heavy hail",
        _ => "Unknown"
    };

    public WeatherCategory GetWeatherCategory(int weatherCode) => weatherCode switch
    {
        0 => WeatherCategory.Clear,
        1 or 2 or 3 => WeatherCategory.Cloudy,
        45 or 48 => WeatherCategory.Fog,
        51 or 53 or 55 => WeatherCategory.Drizzle,
        61 or 63 or 65 or 80 or 81 or 82 => WeatherCategory.Rain,
        71 or 73 or 75 or 77 or 85 or 86 => WeatherCategory.Snow,
        95 or 96 or 99 => WeatherCategory.Thunderstorm,
        _ => WeatherCategory.Unknown
    };
}
