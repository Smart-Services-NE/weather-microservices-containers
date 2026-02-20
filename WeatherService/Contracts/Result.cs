namespace WeatherService.Contracts;

public record Result(bool Success, ErrorInfo? Error = null);

public record Result<T>(bool Success, T? Data = default, ErrorInfo? Error = null) : Result(Success, Error);
