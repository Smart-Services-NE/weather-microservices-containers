using System.Text.Json.Serialization;

namespace WeatherService.Models;

public record Place(
    [property: JsonPropertyName("place name")] string PlaceName,
    [property: JsonPropertyName("longitude")] string Longitude,
    [property: JsonPropertyName("state abbreviation")] string StateAbbreviation,
    [property: JsonPropertyName("latitude")] string Latitude);
