namespace WeatherService.Contracts;

public record DailyForecast(
    string Date,
    double TemperatureMaxF,
    double TemperatureMinF,
    int WeatherCode,
    string Summary,
    int PrecipitationProbability = 0
);
