using System.Text.Json.Serialization;

namespace WeatherService.Models;

public record ZipCodeResponse(
    [property: JsonPropertyName("post code")] string PostCode,
    [property: JsonPropertyName("places")] List<Place> Places);
