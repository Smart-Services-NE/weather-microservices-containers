using FluentAssertions;
using WeatherService.Contracts;
using WeatherService.Engines;
using Xunit;

namespace WeatherService.Engines.Tests;

public class WeatherCodeEngineTests
{
    private readonly WeatherCodeEngine _engine;

    public WeatherCodeEngineTests()
    {
        _engine = new WeatherCodeEngine();
    }

    [Theory]
    [InlineData(0, "Clear sky", WeatherCategory.Clear)]
    [InlineData(1, "Mainly clear, partly cloudy, and overcast", WeatherCategory.Cloudy)]
    [InlineData(2, "Mainly clear, partly cloudy, and overcast", WeatherCategory.Cloudy)]
    [InlineData(3, "Mainly clear, partly cloudy, and overcast", WeatherCategory.Cloudy)]
    [InlineData(45, "Fog", WeatherCategory.Fog)]
    [InlineData(48, "Fog", WeatherCategory.Fog)]
    [InlineData(51, "Drizzle", WeatherCategory.Drizzle)]
    [InlineData(53, "Drizzle", WeatherCategory.Drizzle)]
    [InlineData(55, "Drizzle", WeatherCategory.Drizzle)]
    [InlineData(61, "Rain", WeatherCategory.Rain)]
    [InlineData(63, "Rain", WeatherCategory.Rain)]
    [InlineData(65, "Rain", WeatherCategory.Rain)]
    [InlineData(71, "Snow fall", WeatherCategory.Snow)]
    [InlineData(73, "Snow fall", WeatherCategory.Snow)]
    [InlineData(75, "Snow fall", WeatherCategory.Snow)]
    [InlineData(77, "Snow grains", WeatherCategory.Snow)]
    [InlineData(80, "Rain showers", WeatherCategory.Rain)]
    [InlineData(81, "Rain showers", WeatherCategory.Rain)]
    [InlineData(82, "Rain showers", WeatherCategory.Rain)]
    [InlineData(85, "Snow showers", WeatherCategory.Snow)]
    [InlineData(86, "Snow showers", WeatherCategory.Snow)]
    [InlineData(95, "Thunderstorm", WeatherCategory.Thunderstorm)]
    [InlineData(96, "Thunderstorm with slight and heavy hail", WeatherCategory.Thunderstorm)]
    [InlineData(99, "Thunderstorm with slight and heavy hail", WeatherCategory.Thunderstorm)]
    public void TranslateWeatherCode_ShouldReturnCorrectDescription(int code, string expectedSummary, WeatherCategory expectedCategory)
    {
        var summary = _engine.TranslateWeatherCode(code);
        var category = _engine.GetWeatherCategory(code);

        summary.Should().Be(expectedSummary);
        category.Should().Be(expectedCategory);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100)]
    [InlineData(4)]
    [InlineData(50)]
    [InlineData(60)]
    public void TranslateWeatherCode_WithUnknownCode_ShouldReturnUnknown(int code)
    {
        var summary = _engine.TranslateWeatherCode(code);
        var category = _engine.GetWeatherCategory(code);

        summary.Should().Be("Unknown");
        category.Should().Be(WeatherCategory.Unknown);
    }

    [Fact]
    public void TranslateWeatherCode_WithNegativeCode_ShouldReturnUnknown()
    {
        var summary = _engine.TranslateWeatherCode(-999);
        summary.Should().Be("Unknown");
    }

    [Fact]
    public void TranslateWeatherCode_WithLargeCode_ShouldReturnUnknown()
    {
        var summary = _engine.TranslateWeatherCode(1000);
        summary.Should().Be("Unknown");
    }
}
