using FluentAssertions;
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
}
