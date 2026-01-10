using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Contracts;
using NotificationService.Utilities;
using Xunit;

namespace NotificationService.Utilities.Tests;

public class KafkaProducerUtilityTests : IDisposable
{
    private readonly Mock<ITelemetryUtility> _mockTelemetry;
    private readonly Mock<ILogger<KafkaProducerUtility>> _mockLogger;
    private readonly IConfiguration _configuration;
    private readonly KafkaProducerUtility _sut;

    public KafkaProducerUtilityTests()
    {
        _mockTelemetry = new Mock<ITelemetryUtility>();
        _mockLogger = new Mock<ILogger<KafkaProducerUtility>>();

        // Setup minimal configuration for testing
        var inMemorySettings = new Dictionary<string, string>
        {
            { "Kafka:BootstrapServers", "localhost:9092" },
            { "Kafka:ProducerTopic", "test-topic" },
            { "Kafka:ProducerAcks", "all" },
            { "Kafka:EnableIdempotence", "true" },
            { "Kafka:CompressionType", "gzip" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        _sut = new KafkaProducerUtility(_configuration, _mockTelemetry.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task PublishNotificationRecordAsync_WithNullRecord_ThrowsArgumentNullException()
    {
        // Arrange
        NotificationRecord? record = null;

        // Act
        Func<Task> act = async () => await _sut.PublishNotificationRecordAsync(record!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("record");
    }

    [Fact]
    public async Task PublishNotificationRecordAsync_WithValidRecord_StartsTelemetryActivity()
    {
        // Arrange
        var record = CreateTestRecord();
        _mockTelemetry.Setup(t => t.StartActivity(It.IsAny<string>()))
            .Returns((System.Diagnostics.Activity?)null);

        // Act
        // Note: This will fail to actually publish since we don't have a real Kafka broker,
        // but we can verify telemetry setup happens before the publish attempt
        try
        {
            await _sut.PublishNotificationRecordAsync(record, CancellationToken.None);
        }
        catch
        {
            // Expected to fail without real Kafka broker
        }

        // Assert
        _mockTelemetry.Verify(
            t => t.StartActivity("PublishNotificationRecord"),
            Times.Once);
    }

    [Fact]
    public async Task PublishNotificationRecordAsync_WithValidRecord_SetsTelemetryTags()
    {
        // Arrange
        var record = CreateTestRecord();
        _mockTelemetry.Setup(t => t.StartActivity(It.IsAny<string>()))
            .Returns((System.Diagnostics.Activity?)null);

        // Act
        try
        {
            await _sut.PublishNotificationRecordAsync(record, CancellationToken.None);
        }
        catch
        {
            // Expected to fail without real Kafka broker
        }

        // Assert
        _mockTelemetry.Verify(
            t => t.SetTag("topic", "test-topic"),
            Times.AtLeastOnce);
        _mockTelemetry.Verify(
            t => t.SetTag("messageId", record.MessageId),
            Times.AtLeastOnce);
        _mockTelemetry.Verify(
            t => t.SetTag("status", record.Status.ToString()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void Constructor_WithValidConfiguration_InitializesSuccessfully()
    {
        // Arrange & Act
        using var producer = new KafkaProducerUtility(
            _configuration,
            _mockTelemetry.Object,
            _mockLogger.Object);

        // Assert
        producer.Should().NotBeNull();
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Kafka producer initialized")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_WithoutProducerTopic_UsesDefaultTopic()
    {
        // Arrange
        var configWithoutTopic = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "Kafka:BootstrapServers", "localhost:9092" }
            }!)
            .Build();

        // Act
        using var producer = new KafkaProducerUtility(
            configWithoutTopic,
            _mockTelemetry.Object,
            _mockLogger.Object);

        // Assert
        producer.Should().NotBeNull();
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("notification-records")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_WithSecurityProtocol_ConfiguresSecurity()
    {
        // Arrange
        var secureConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "Kafka:BootstrapServers", "localhost:9092" },
                { "Kafka:SecurityProtocol", "SaslSsl" },
                { "Kafka:SaslMechanism", "Plain" },
                { "Kafka:SaslUsername", "test-user" },
                { "Kafka:SaslPassword", "test-password" }
            }!)
            .Build();

        // Act
        using var producer = new KafkaProducerUtility(
            secureConfig,
            _mockTelemetry.Object,
            _mockLogger.Object);

        // Assert
        producer.Should().NotBeNull();
    }

    [Fact]
    public void Dispose_CallsFlushOnProducer()
    {
        // Arrange
        var producer = new KafkaProducerUtility(
            _configuration,
            _mockTelemetry.Object,
            _mockLogger.Object);

        // Act
        producer.Dispose();

        // Assert
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Disposing Kafka producer")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_OnlyDisposesOnce()
    {
        // Arrange
        var producer = new KafkaProducerUtility(
            _configuration,
            _mockTelemetry.Object,
            _mockLogger.Object);

        // Act
        producer.Dispose();
        producer.Dispose();
        producer.Dispose();

        // Assert
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Disposing Kafka producer")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once, // Should only log once even with multiple Dispose calls
            "Dispose should be idempotent");
    }

    private static NotificationRecord CreateTestRecord()
    {
        return new NotificationRecord(
            Id: Guid.NewGuid(),
            MessageId: "test-message-123",
            Topic: "weather-alerts",
            Subject: "Test Subject",
            Body: "Test body content",
            Recipient: "test@example.com",
            Status: NotificationStatus.Sent,
            CreatedAt: DateTime.UtcNow,
            SentAt: DateTime.UtcNow,
            RetryCount: 0,
            ErrorMessage: null
        );
    }

    public void Dispose()
    {
        _sut?.Dispose();
    }
}
