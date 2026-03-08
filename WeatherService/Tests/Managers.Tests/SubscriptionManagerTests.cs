using Moq;
using FluentAssertions;
using WeatherService.Contracts;
using WeatherService.Managers;
using Xunit;

namespace WeatherService.Managers.Tests;

public class SubscriptionManagerTests
{
    private readonly Mock<ISubscriptionAccessor> _mockSubscriptionAccessor;
    private readonly Mock<IWeatherManager> _mockWeatherManager;
    private readonly Mock<IWeatherAlertEngine> _mockAlertEngine;
    private readonly Mock<IAlertPublisherAccessor> _mockAlertPublisher;
    private readonly Mock<ITelemetryUtility> _mockTelemetry;
    private readonly SubscriptionManager _manager;

    public SubscriptionManagerTests()
    {
        _mockSubscriptionAccessor = new Mock<ISubscriptionAccessor>();
        _mockWeatherManager = new Mock<IWeatherManager>();
        _mockAlertEngine = new Mock<IWeatherAlertEngine>();
        _mockAlertPublisher = new Mock<IAlertPublisherAccessor>();
        _mockTelemetry = new Mock<ITelemetryUtility>();

        _mockTelemetry
            .Setup(x => x.StartActivity(It.IsAny<string>()))
            .Returns(new TestDisposable());

        _manager = new SubscriptionManager(
            _mockSubscriptionAccessor.Object,
            _mockWeatherManager.Object,
            _mockAlertEngine.Object,
            _mockAlertPublisher.Object,
            _mockTelemetry.Object,
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<SubscriptionManager>()
        );
    }

    [Fact]
    public async Task SubscribeAsync_ShouldCreateSubscription()
    {
        var request = new SubscriptionRequest("test@email.com", "68136", ThresholdType.TemperatureLow, 32.0, "less-than");
        _mockSubscriptionAccessor
            .Setup(x => x.CreateSubscriptionAsync(It.IsAny<SubscriptionRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result(true));

        var result = await _manager.SubscribeAsync(request, CancellationToken.None);

        result.Success.Should().BeTrue();
        _mockSubscriptionAccessor.Verify(x => x.CreateSubscriptionAsync(
            It.Is<SubscriptionRecord>(r => r.Email == request.Email && r.ZipCode == request.ZipCode),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessSubscriptionsAsync_ShouldNotifyEachSubscriber()
    {
        var subs = new List<SubscriptionRecord>
        {
            new SubscriptionRecord(Guid.NewGuid(), "user1@test.com", "11111", ThresholdType.TemperatureLow, 32.0, "less-than", DateTime.UtcNow),
            new SubscriptionRecord(Guid.NewGuid(), "user2@test.com", "22222", ThresholdType.TemperatureLow, 32.0, "less-than", DateTime.UtcNow)
        };

        _mockSubscriptionAccessor
            .Setup(x => x.GetAllSubscriptionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(subs);

        _mockWeatherManager
            .Setup(x => x.GetWeatherForecastAsync(It.IsAny<string>()))
            .ReturnsAsync(new WeatherForecastResult(true, new WeatherForecastData("City", "ST", "11111", 30, "Summary", "Date", new GeoLocation("0", "0"), null, null), null));

        _mockAlertEngine
            .Setup(x => x.EvaluateThreshold(It.IsAny<ThresholdType>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<string>()))
            .Returns(true);

        _mockAlertPublisher
            .Setup(x => x.PublishSentinelAlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ThresholdType>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result(true));

        var result = await _manager.ProcessSubscriptionsAsync(CancellationToken.None);

        result.Success.Should().BeTrue();
        _mockAlertPublisher.Verify(x => x.PublishSentinelAlertAsync("user1@test.com", "11111", It.IsAny<ThresholdType>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockAlertPublisher.Verify(x => x.PublishSentinelAlertAsync("user2@test.com", "22222", It.IsAny<ThresholdType>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private class TestDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
