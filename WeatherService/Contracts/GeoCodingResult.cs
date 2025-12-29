namespace WeatherService.Contracts;

public record GeoCodingResult(
    bool Success,
    string? City,
    string? State,
    string? Latitude,
    string? Longitude,
    ErrorInfo? Error
);
