using System.Text.Json.Serialization;

namespace WeatherService.Accessors.Models;

public record ZipCodeResponse(
    [property: JsonPropertyName("post code")] string PostCode,
    [property: JsonPropertyName("places")] List<Place> Places
);

public record Place(
    [property: JsonPropertyName("place name")] string PlaceName,
    [property: JsonPropertyName("latitude")] string Latitude,
    [property: JsonPropertyName("longitude")] string Longitude,
    [property: JsonPropertyName("state abbreviation")] string StateAbbreviation
);
