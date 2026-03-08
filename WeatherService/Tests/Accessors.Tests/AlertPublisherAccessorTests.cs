using Moq;
using FluentAssertions;
using Dapr.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using WeatherService.Accessors;
using WeatherService.Contracts;
using Xunit;

namespace WeatherService.Accessors.Tests;

public class AlertPublisherAccessorTests
{
    private readonly Mock<DaprClient> _mockDapr;
    private readonly Mock<IRetryPolicyUtility> _mockRetry;
    private readonly Mock<ITelemetryUtility> _mockTelemetry;
    private readonly Mock<ILogger<AlertPublisherAccessor>> _mockLogger;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _config;
    private readonly AlertPublisherAccessor _accessor;

    public AlertPublisherAccessorTests()
    {
        _mockDapr = new Mock<DaprClient>();
        _mockRetry = new Mock<IRetryPolicyUtility>();
        _mockTelemetry = new Mock<ITelemetryUtility>();
        _mockLogger = new Mock<ILogger<AlertPublisherAccessor>>();
        
        // Use a real configuration instead of a mock to handle extension methods like GetValue
        _config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kafka:UseAvro"] = "false"
            })
            .Build();

        // Setup retry logic to execute immediately
        _mockRetry
            .Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((op, ct) => op(ct));

        _accessor = new AlertPublisherAccessor(
            _mockDapr.Object,
            _mockRetry.Object,
            _mockTelemetry.Object,
            _mockLogger.Object,
            _config);
    }

    [Fact]
    public async Task PublishFreezingAlertAsync_ShouldPublishCorrectWeatherAlertDto()
    {
        // Arrange
        var email = "test@example.com";
        var zipCode = "90210";
        var temperature = -5.5;

        // Act
        var result = await _accessor.PublishFreezingAlertAsync(email, zipCode, temperature, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();

        _mockDapr.Verify(x => x.PublishEventAsync(
            "pubsub",
            "weather-alerts",
            It.Is<WeatherAlertDto>(a => 
                a.Recipient == email &&
                a.Location!.ZipCode == zipCode &&
                a.WeatherConditions!.CurrentTemperature == temperature &&
                a.AlertType == "TEMPERATURE_EXTREME" &&
                a.Severity == "WARNING"
            ),
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }
}
