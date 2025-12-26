using System.Text.Json.Serialization;

namespace WeatherService.Models;

public record OpenMeteoResponse(
    [property: JsonPropertyName("current_weather")] CurrentWeather CurrentWeather);
