namespace WeatherService.Contracts;

public record FreezingAlertRequest(string ZipCode, string Email);
