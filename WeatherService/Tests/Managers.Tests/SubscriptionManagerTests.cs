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
    private readonly Mock<ITelemetryUtility> _mockTelemetry;
    private readonly SubscriptionManager _manager;

    public SubscriptionManagerTests()
    {
        _mockSubscriptionAccessor = new Mock<ISubscriptionAccessor>();
        _mockWeatherManager = new Mock<IWeatherManager>();
        _mockTelemetry = new Mock<ITelemetryUtility>();

        _mockTelemetry
            .Setup(x => x.StartActivity(It.IsAny<string>()))
            .Returns(new TestDisposable());

        _manager = new SubscriptionManager(
            _mockSubscriptionAccessor.Object,
            _mockWeatherManager.Object,
            _mockTelemetry.Object,
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<SubscriptionManager>()
        );
    }

    [Fact]
    public async Task SubscribeAsync_ShouldCreateSubscription()
    {
        var request = new SubscriptionRequest("test@email.com", "68136");
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
            new SubscriptionRecord(Guid.NewGuid(), "user1@test.com", "11111", DateTime.UtcNow),
            new SubscriptionRecord(Guid.NewGuid(), "user2@test.com", "22222", DateTime.UtcNow)
        };

        _mockSubscriptionAccessor
            .Setup(x => x.GetAllSubscriptionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(subs);

        _mockWeatherManager
            .Setup(x => x.NotifyIfFreezingAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result(true));

        var result = await _manager.ProcessSubscriptionsAsync(CancellationToken.None);

        result.Success.Should().BeTrue();
        _mockWeatherManager.Verify(x => x.NotifyIfFreezingAsync("11111", "user1@test.com", It.IsAny<CancellationToken>()), Times.Once);
        _mockWeatherManager.Verify(x => x.NotifyIfFreezingAsync("22222", "user2@test.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    private class TestDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
