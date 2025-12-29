namespace WeatherService.Contracts;

public record LocationValidationResult(
    bool IsValid,
    string? City,
    string? State,
    ErrorInfo? Error
);
