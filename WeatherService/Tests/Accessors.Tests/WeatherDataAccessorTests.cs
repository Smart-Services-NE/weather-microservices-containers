using FluentAssertions;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WeatherService.Accessors;
using Xunit;

namespace WeatherService.Accessors.Tests;

public class WeatherDataAccessorTests : IDisposable
{
    private readonly WireMockServer _mockServer;
    private readonly WeatherDataAccessor _accessor;

    public WeatherDataAccessorTests()
    {
        _mockServer = WireMockServer.Start();
        _accessor = new WeatherDataAccessor(new TestHttpClientFactory(_mockServer.Url!));
    }

    [Fact]
    public async Task GetCurrentWeatherAsync_WithValidCoordinates_ShouldReturnSuccessResult()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/v1/forecast")
                .WithParam("latitude", "41.2586")
                .WithParam("longitude", "-96.0025")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""current_weather"": {
                        ""temperature"": 72.5,
                        ""weathercode"": 0
                    }
                }"));

        var result = await _accessor.GetCurrentWeatherAsync("41.2586", "-96.0025");

        result.Success.Should().BeTrue();
        result.TemperatureF.Should().Be(72.5);
        result.WeatherCode.Should().Be(0);
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentWeatherAsync_WithServerError_ShouldReturnApiError()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/v1/forecast")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(500));

        var result = await _accessor.GetCurrentWeatherAsync("41.2586", "-96.0025");

        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("WEATHER_API_ERROR");
    }

    [Fact]
    public async Task GetCurrentWeatherAsync_WithNullResponse_ShouldReturnNoDataError()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/v1/forecast")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("null"));

        var result = await _accessor.GetCurrentWeatherAsync("41.2586", "-96.0025");

        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("WEATHER_NO_DATA");
    }

    [Fact]
    public async Task GetCurrentWeatherAsync_WithNetworkError_ShouldReturnNetworkError()
    {
        var invalidAccessor = new WeatherDataAccessor(new TestHttpClientFactory("http://invalid-host-that-does-not-exist:9999"));

        var result = await invalidAccessor.GetCurrentWeatherAsync("41.2586", "-96.0025");

        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("WEATHER_NETWORK_ERROR");
    }

    [Fact]
    public async Task GetCurrentWeatherAsync_WithDifferentWeatherCodes_ShouldReturnCorrectData()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/v1/forecast")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""current_weather"": {
                        ""temperature"": 45.3,
                        ""weathercode"": 61
                    }
                }"));

        var result = await _accessor.GetCurrentWeatherAsync("41.2586", "-96.0025");

        result.Success.Should().BeTrue();
        result.TemperatureF.Should().Be(45.3);
        result.WeatherCode.Should().Be(61);
    }

    public void Dispose()
    {
        _mockServer?.Stop();
        _mockServer?.Dispose();
    }

    private class TestHttpClientFactory : IHttpClientFactory
    {
        private readonly string _baseUrl;

        public TestHttpClientFactory(string baseUrl)
        {
            _baseUrl = baseUrl;
        }

        public HttpClient CreateClient(string name)
        {
            return new HttpClient { BaseAddress = new Uri(_baseUrl) };
        }
    }
}
