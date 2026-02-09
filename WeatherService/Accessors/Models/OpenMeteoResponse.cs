using System.Text.Json.Serialization;

namespace WeatherService.Accessors.Models;

public record OpenMeteoResponse(
    [property: JsonPropertyName("current_weather")] CurrentWeather CurrentWeather,
    [property: JsonPropertyName("hourly")] HourlyData? Hourly,
    [property: JsonPropertyName("daily")] DailyData? Daily
);

public record CurrentWeather(
    [property: JsonPropertyName("temperature")] double Temperature,
    [property: JsonPropertyName("weathercode")] int WeatherCode
);

public record HourlyData(
    [property: JsonPropertyName("time")] string[] Time,
    [property: JsonPropertyName("temperature_2m")] double[] Temperature2m,
    [property: JsonPropertyName("weathercode")] int[] WeatherCode
);

public record DailyData(
    [property: JsonPropertyName("time")] string[] Time,
    [property: JsonPropertyName("temperature_2m_max")] double[] Temperature2mMax,
    [property: JsonPropertyName("temperature_2m_min")] double[] Temperature2mMin,
    [property: JsonPropertyName("weathercode")] int[] WeatherCode
);
