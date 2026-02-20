using FluentAssertions;
using Moq;
using WeatherService.Contracts;
using WeatherService.Managers;
using Xunit;

namespace WeatherService.Managers.Tests;

public class WeatherManagerTests
{
    private readonly Mock<IGeoCodingAccessor> _mockGeoCoding;
    private readonly Mock<IWeatherDataAccessor> _mockWeatherData;
    private readonly Mock<IWeatherCodeEngine> _mockWeatherCode;
    private readonly Mock<ICacheUtility> _mockCache;
    private readonly Mock<IWeatherAlertEngine> _mockAlertEngine;
    private readonly Mock<IAlertPublisherAccessor> _mockAlertPublisher;
    private readonly Mock<ITelemetryUtility> _mockTelemetry;
    private readonly WeatherManager _manager;

    public WeatherManagerTests()
    {
        _mockGeoCoding = new Mock<IGeoCodingAccessor>();
        _mockWeatherData = new Mock<IWeatherDataAccessor>();
        _mockWeatherCode = new Mock<IWeatherCodeEngine>();
        _mockAlertEngine = new Mock<IWeatherAlertEngine>();
        _mockAlertPublisher = new Mock<IAlertPublisherAccessor>();
        _mockCache = new Mock<ICacheUtility>();
        _mockTelemetry = new Mock<ITelemetryUtility>();

        _mockTelemetry
            .Setup(x => x.StartActivity(It.IsAny<string>()))
            .Returns(new TestDisposable());

        _manager = new WeatherManager(
            _mockGeoCoding.Object,
            _mockWeatherData.Object,
            _mockWeatherCode.Object,
            _mockAlertEngine.Object,
            _mockAlertPublisher.Object,
            _mockCache.Object,
            _mockTelemetry.Object
        );
    }

    [Fact]
    public async Task GetWeatherForecastAsync_WithCachedData_ShouldReturnCachedResult()
    {
        var cachedData = new WeatherForecastData(
            "Omaha",
            "NE",
            "68136",
            72,
            "Clear sky",
            "2025-12-29",
            new GeoLocation("41.2586", "-96.0025"),
            null,
            null
        );

        _mockCache
            .Setup(x => x.GetAsync<WeatherForecastData>("weather:68136"))
            .ReturnsAsync(cachedData);

        var result = await _manager.GetWeatherForecastAsync("68136");

        result.Success.Should().BeTrue();
        result.Data.Should().Be(cachedData);
        result.Error.Should().BeNull();

        _mockTelemetry.Verify(x => x.SetTag("weather.source", "cache"), Times.Once);
        _mockGeoCoding.Verify(x => x.GetLocationByZipCodeAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetWeatherForecastAsync_WithCacheMiss_ShouldFetchFromUpstream()
    {
        _mockCache
            .Setup(x => x.GetAsync<WeatherForecastData>(It.IsAny<string>()))
            .ReturnsAsync((WeatherForecastData?)null);

        _mockGeoCoding
            .Setup(x => x.GetLocationByZipCodeAsync("68136"))
            .ReturnsAsync(new GeoCodingResult(true, "Omaha", "NE", "41.2586", "-96.0025", null));

        _mockWeatherData
            .Setup(x => x.GetCurrentWeatherAsync("41.2586", "-96.0025"))
            .ReturnsAsync(new WeatherDataResult(true, 72.5, 0, null, null, null));

        _mockWeatherCode
            .Setup(x => x.TranslateWeatherCode(0))
            .Returns("Clear sky");

        var result = await _manager.GetWeatherForecastAsync("68136");

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.City.Should().Be("Omaha");
        result.Data.State.Should().Be("NE");
        result.Data.TemperatureF.Should().Be(72);
        result.Data.Summary.Should().Be("Clear sky");

        _mockTelemetry.Verify(x => x.SetTag("weather.source", "upstream"), Times.Once);
        _mockCache.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<WeatherForecastData>(), It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Fact]
    public async Task GetWeatherForecastAsync_WhenGeoCodingFails_ShouldReturnError()
    {
        _mockCache
            .Setup(x => x.GetAsync<WeatherForecastData>(It.IsAny<string>()))
            .ReturnsAsync((WeatherForecastData?)null);

        var error = new ErrorInfo("ZIPCODE_NOT_FOUND", "Zipcode not found");
        _mockGeoCoding
            .Setup(x => x.GetLocationByZipCodeAsync("00000"))
            .ReturnsAsync(new GeoCodingResult(false, null, null, null, null, error));

        var result = await _manager.GetWeatherForecastAsync("00000");

        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Error.Should().Be(error);

        _mockWeatherData.Verify(x => x.GetCurrentWeatherAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetWeatherForecastAsync_WhenWeatherDataFails_ShouldReturnError()
    {
        _mockCache
            .Setup(x => x.GetAsync<WeatherForecastData>(It.IsAny<string>()))
            .ReturnsAsync((WeatherForecastData?)null);

        _mockGeoCoding
            .Setup(x => x.GetLocationByZipCodeAsync("68136"))
            .ReturnsAsync(new GeoCodingResult(true, "Omaha", "NE", "41.2586", "-96.0025", null));

        var error = new ErrorInfo("WEATHER_API_ERROR", "Weather API error");
        _mockWeatherData
            .Setup(x => x.GetCurrentWeatherAsync("41.2586", "-96.0025"))
            .ReturnsAsync(new WeatherDataResult(false, null, null, null, null, error));

        var result = await _manager.GetWeatherForecastAsync("68136");

        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Error.Should().Be(error);

        _mockWeatherCode.Verify(x => x.TranslateWeatherCode(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ValidateLocationAsync_WithValidZipCode_ShouldReturnValid()
    {
        _mockGeoCoding
            .Setup(x => x.GetLocationByZipCodeAsync("68136"))
            .ReturnsAsync(new GeoCodingResult(true, "Omaha", "NE", "41.2586", "-96.0025", null));

        var result = await _manager.ValidateLocationAsync("68136");

        result.IsValid.Should().BeTrue();
        result.City.Should().Be("Omaha");
        result.State.Should().Be("NE");
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task ValidateLocationAsync_WithInvalidZipCode_ShouldReturnInvalid()
    {
        var error = new ErrorInfo("ZIPCODE_NOT_FOUND", "Zipcode not found");
        _mockGeoCoding
            .Setup(x => x.GetLocationByZipCodeAsync("00000"))
            .ReturnsAsync(new GeoCodingResult(false, null, null, null, null, error));

        var result = await _manager.ValidateLocationAsync("00000");

        result.IsValid.Should().BeFalse();
        result.Error.Should().Be(error);
    }

    [Fact]
    public async Task GetCachedForecastAsync_WithCachedData_ShouldReturnData()
    {
        var cachedData = new WeatherForecastData(
            "Omaha",
            "NE",
            "68136",
            72,
            "Clear sky",
            "2025-12-29",
            new GeoLocation("41.2586", "-96.0025"),
            null,
            null
        );

        _mockCache
            .Setup(x => x.GetAsync<WeatherForecastData>("weather:68136"))
            .ReturnsAsync(cachedData);

        var result = await _manager.GetCachedForecastAsync("68136");

        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().Be(cachedData);
        _mockTelemetry.Verify(x => x.SetTag("cache.hit", true), Times.Once);
    }

    [Fact]
    public async Task GetCachedForecastAsync_WithNoCachedData_ShouldReturnNull()
    {
        _mockCache
            .Setup(x => x.GetAsync<WeatherForecastData>("weather:68136"))
            .ReturnsAsync((WeatherForecastData?)null);

        var result = await _manager.GetCachedForecastAsync("68136");

        result.Should().BeNull();
        _mockTelemetry.Verify(x => x.SetTag("cache.hit", false), Times.Once);
    }

    [Fact]
    public async Task NotifyIfFreezingAsync_WhenFreezing_ShouldPublishAlert()
    {
        // Arrange
        var zipCode = "68136";
        var email = "test@example.com";
        _mockGeoCoding
            .Setup(x => x.GetLocationByZipCodeAsync(zipCode))
            .ReturnsAsync(new GeoCodingResult(true, "Omaha", "NE", "41.2586", "-96.0025", null));

        _mockWeatherData
            .Setup(x => x.GetCurrentWeatherAsync("41.2586", "-96.0025"))
            .ReturnsAsync(new WeatherDataResult(true, 30.0, 0, null, null, null)); // 30F is freezing

        _mockAlertEngine
            .Setup(x => x.IsFreezing(It.IsAny<double>()))
            .Returns(true);

        _mockAlertPublisher
            .Setup(x => x.PublishFreezingAlertAsync(email, zipCode, It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result(true));

        // Act
        var result = await _manager.NotifyIfFreezingAsync(zipCode, email, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _mockAlertPublisher.Verify(x => x.PublishFreezingAlertAsync(email, zipCode, It.IsAny<double>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockTelemetry.Verify(x => x.SetTag("alert.sent", true), Times.Once);
    }

    private class TestDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
