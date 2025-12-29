using System.Text.Json.Serialization;

namespace WeatherService.Accessors.Models;

public record OpenMeteoResponse(
    [property: JsonPropertyName("current_weather")] CurrentWeather CurrentWeather
);

public record CurrentWeather(
    [property: JsonPropertyName("temperature")] double Temperature,
    [property: JsonPropertyName("weathercode")] int WeatherCode
);
