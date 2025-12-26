using System.Text.Json.Serialization;

namespace WeatherService.Models;

public record CurrentWeather(
    [property: JsonPropertyName("temperature")] double Temperature,
    [property: JsonPropertyName("weathercode")] int WeatherCode);
