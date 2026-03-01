using FluentAssertions;
using WeatherService.Contracts;
using WeatherService.Engines;
using Xunit;

namespace WeatherService.Tests.Engines;

public class WeatherAlertEngineTests
{
    private readonly WeatherAlertEngine _sut;

    public WeatherAlertEngineTests()
    {
        _sut = new WeatherAlertEngine();
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(-1, true)]
    [InlineData(-15.5, true)]
    [InlineData(0.1, false)]
    [InlineData(32, false)]
    public void IsFreezing_ShouldReturnExpectedResult(double temperature, bool expected)
    {
        // Act
        var result = _sut.IsFreezing(temperature);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(ThresholdType.WindSpeed, 35, 30, "greater-than", true)]
    [InlineData(ThresholdType.WindSpeed, 25, 30, "greater-than", false)]
    [InlineData(ThresholdType.TemperatureHigh, 95, 90, "greater-than", true)]
    [InlineData(ThresholdType.TemperatureLow, 25, 32, "less-than", true)]
    [InlineData(ThresholdType.PrecipitationProbability, 40, 50, "greater-than", false)]
    public void EvaluateThreshold_ShouldReturnExpectedResult(
        ThresholdType type, double current, double threshold, string op, bool expected)
    {
        // Act
        var result = _sut.EvaluateThreshold(type, current, threshold, op);

        // Assert
        result.Should().Be(expected);
    }
}
