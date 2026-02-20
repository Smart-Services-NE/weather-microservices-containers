using Moq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WeatherService.Accessors;
using WeatherService.Contracts;
using Xunit;

namespace WeatherService.Accessors.Tests;

public class GeoCodingAccessorTests : IDisposable
{
    private readonly WireMockServer _mockServer;
    private readonly Mock<IRetryPolicyUtility> _mockRetry;
    private readonly Mock<ITelemetryUtility> _mockTelemetry;
    private readonly GeoCodingAccessor _accessor;
    public GeoCodingAccessorTests()
    {
        _mockServer = WireMockServer.Start();
        var httpClient = new HttpClient { BaseAddress = new Uri(_mockServer.Url!) };
        _mockRetry = new Mock<IRetryPolicyUtility>();
        _mockTelemetry = new Mock<ITelemetryUtility>();

        // Setup retry logic to execute immediately
        _mockRetry
            .Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<CancellationToken, Task<HttpResponseMessage>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<HttpResponseMessage>>, CancellationToken>((op, ct) => op(ct));

        _accessor = new GeoCodingAccessor(httpClient, _mockRetry.Object, _mockTelemetry.Object);
    }

    [Fact]
    public async Task GetLocationByZipCodeAsync_WithValidZipCode_ShouldReturnSuccessResult()
    {
        _mockServer
            .Given(Request.Create().WithPath("/us/68136").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""post code"": ""68136"",
                    ""places"": [{
                        ""place name"": ""Omaha"",
                        ""latitude"": ""41.2586"",
                        ""longitude"": ""-96.0025"",
                        ""state abbreviation"": ""NE""
                    }]
                }"));

        var result = await _accessor.GetLocationByZipCodeAsync("68136");

        result.Success.Should().BeTrue();
        result.City.Should().Be("Omaha");
        result.State.Should().Be("NE");
        result.Latitude.Should().Be("41.2586");
        result.Longitude.Should().Be("-96.0025");
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task GetLocationByZipCodeAsync_WithInvalidZipCode_ShouldReturnNotFoundError()
    {
        _mockServer
            .Given(Request.Create().WithPath("/us/00000").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(404));

        var result = await _accessor.GetLocationByZipCodeAsync("00000");

        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("ZIPCODE_NOT_FOUND");
        result.Error.Message.Should().Contain("00000");
    }

    [Fact]
    public async Task GetLocationByZipCodeAsync_WithServerError_ShouldReturnApiError()
    {
        _mockServer
            .Given(Request.Create().WithPath("/us/68136").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(500));

        var result = await _accessor.GetLocationByZipCodeAsync("68136");

        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("GEOCODING_API_ERROR");
    }

    [Fact]
    public async Task GetLocationByZipCodeAsync_WithEmptyPlaces_ShouldReturnNoDataError()
    {
        _mockServer
            .Given(Request.Create().WithPath("/us/68136").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""post code"": ""68136"",
                    ""places"": []
                }"));

        var result = await _accessor.GetLocationByZipCodeAsync("68136");

        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("GEOCODING_NO_DATA");
    }

    [Fact]
    public async Task GetLocationByZipCodeAsync_WithNetworkError_ShouldReturnNetworkError()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri("http://invalid-host-that-does-not-exist:9999") };
        var invalidAccessor = new GeoCodingAccessor(httpClient, _mockRetry.Object, _mockTelemetry.Object);

        var result = await invalidAccessor.GetLocationByZipCodeAsync("68136");

        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("GEOCODING_NETWORK_ERROR");
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
