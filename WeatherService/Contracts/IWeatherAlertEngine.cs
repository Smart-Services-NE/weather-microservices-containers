namespace WeatherService.Contracts;

public interface IWeatherAlertEngine
{
    bool IsFreezing(double temperatureCelsius);
    bool EvaluateThreshold(ThresholdType type, double currentValue, double thresholdValue, string comparisonOperator);
}
