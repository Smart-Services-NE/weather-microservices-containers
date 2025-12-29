using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WeatherService.Accessors;
using Xunit;

namespace WeatherService.Accessors.Tests;

public class GeoCodingAccessorTests : IDisposable
{
    private readonly WireMockServer _mockServer;
    private readonly GeoCodingAccessor _accessor;
    private readonly IHttpClientFactory _httpClientFactory;

    public GeoCodingAccessorTests()
    {
        _mockServer = WireMockServer.Start();

        var services = new ServiceCollection();
        services.AddHttpClient("default", client =>
        {
            client.BaseAddress = new Uri(_mockServer.Url!);
        });
        var serviceProvider = services.BuildServiceProvider();
        _httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        _accessor = new GeoCodingAccessor(new TestHttpClientFactory(_mockServer.Url!));
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
        var invalidAccessor = new GeoCodingAccessor(new TestHttpClientFactory("http://invalid-host-that-does-not-exist:9999"));

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
