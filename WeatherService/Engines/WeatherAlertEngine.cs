using WeatherService.Contracts;

namespace WeatherService.Engines;

public class WeatherAlertEngine : IWeatherAlertEngine
{
    public bool IsFreezing(double temperatureCelsius)
    {
        return temperatureCelsius <= 0;
    }

    public bool EvaluateThreshold(ThresholdType type, double currentValue, double thresholdValue, string comparisonOperator)
    {
        return comparisonOperator.ToLower() switch
        {
            "greater-than" => currentValue > thresholdValue,
            "less-than" => currentValue < thresholdValue,
            "greater-than-or-equal" => currentValue >= thresholdValue,
            "less-than-or-equal" => currentValue <= thresholdValue,
            _ => false
        };
    }
}
