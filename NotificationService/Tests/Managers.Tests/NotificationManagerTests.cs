using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Contracts;
using NotificationService.Managers;
using Xunit;

namespace NotificationService.Managers.Tests;

public class NotificationManagerTests
{
    private readonly Mock<INotificationEngine> _mockEngine;
    private readonly Mock<IEmailAccessor> _mockEmailAccessor;
    private readonly Mock<INotificationStorageAccessor> _mockStorageAccessor;
    private readonly Mock<IRetryPolicyUtility> _mockRetryPolicy;
    private readonly Mock<ITelemetryUtility> _mockTelemetry;
    private readonly Mock<IKafkaProducerUtility> _mockKafkaProducer;
    private readonly Mock<ILogger<NotificationManager>> _mockLogger;
    private readonly NotificationManager _sut;

    public NotificationManagerTests()
    {
        _mockEngine = new Mock<INotificationEngine>();
        _mockEmailAccessor = new Mock<IEmailAccessor>();
        _mockStorageAccessor = new Mock<INotificationStorageAccessor>();
        _mockRetryPolicy = new Mock<IRetryPolicyUtility>();
        _mockTelemetry = new Mock<ITelemetryUtility>();
        _mockKafkaProducer = new Mock<IKafkaProducerUtility>();
        _mockLogger = new Mock<ILogger<NotificationManager>>();

        _sut = new NotificationManager(
            _mockEngine.Object,
            _mockEmailAccessor.Object,
            _mockStorageAccessor.Object,
            _mockRetryPolicy.Object,
            _mockTelemetry.Object,
            _mockKafkaProducer.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ProcessAndSendNotificationAsync_WithInvalidMessage_ReturnsFailure()
    {
        // Arrange
        var message = CreateTestMessage();
        _mockEngine.Setup(e => e.ValidateMessage(message)).Returns(false);

        // Act
        var result = await _sut.ProcessAndSendNotificationAsync(message, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("INVALID_MESSAGE");
    }

    [Fact]
    public async Task ProcessAndSendNotificationAsync_WithDuplicateMessage_ReturnsExistingRecord()
    {
        // Arrange
        var message = CreateTestMessage();
        var existingRecord = CreateTestRecord();

        _mockEngine.Setup(e => e.ValidateMessage(message)).Returns(true);
        _mockStorageAccessor
            .Setup(s => s.GetByMessageIdAsync(message.MessageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRecord);

        // Act
        var result = await _sut.ProcessAndSendNotificationAsync(message, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Record.Should().Be(existingRecord);
        _mockEmailAccessor.Verify(e => e.SendEmailAsync(It.IsAny<EmailRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAndSendNotificationAsync_SuccessfulSend_PublishesToKafka()
    {
        // Arrange
        var message = CreateTestMessage();
        var createdRecord = CreateTestRecord() with { Status = NotificationStatus.Pending };
        var sentRecord = createdRecord with { Status = NotificationStatus.Sent, SentAt = DateTime.UtcNow };
        var emailRequest = new EmailRequest("test@example.com", "Subject", "Body");

        _mockEngine.Setup(e => e.ValidateMessage(message)).Returns(true);
        _mockStorageAccessor
            .Setup(s => s.GetByMessageIdAsync(message.MessageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationRecord?)null);
        _mockStorageAccessor
            .Setup(s => s.CreateAsync(It.IsAny<NotificationRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdRecord);
        _mockEngine.Setup(e => e.BuildEmailRequest(message)).Returns(emailRequest);
        _mockRetryPolicy
            .Setup(r => r.ExecuteWithRetryAsync(It.IsAny<Func<CancellationToken, Task<NotificationResult>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationResult(Success: true));
        _mockStorageAccessor
            .Setup(s => s.UpdateAsync(It.IsAny<NotificationRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sentRecord);

        // Act
        var result = await _sut.ProcessAndSendNotificationAsync(message, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _mockKafkaProducer.Verify(
            k => k.PublishNotificationRecordAsync(
                It.Is<NotificationRecord>(r => r.Status == NotificationStatus.Sent),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessAndSendNotificationAsync_FailedSend_PublishesFailedRecordToKafka()
    {
        // Arrange
        var message = CreateTestMessage();
        var createdRecord = CreateTestRecord() with { Status = NotificationStatus.Pending };
        var failedRecord = createdRecord with { Status = NotificationStatus.Failed, ErrorMessage = "Send failed" };
        var emailRequest = new EmailRequest("test@example.com", "Subject", "Body");

        _mockEngine.Setup(e => e.ValidateMessage(message)).Returns(true);
        _mockStorageAccessor
            .Setup(s => s.GetByMessageIdAsync(message.MessageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationRecord?)null);
        _mockStorageAccessor
            .Setup(s => s.CreateAsync(It.IsAny<NotificationRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdRecord);
        _mockEngine.Setup(e => e.BuildEmailRequest(message)).Returns(emailRequest);
        _mockRetryPolicy
            .Setup(r => r.ExecuteWithRetryAsync(It.IsAny<Func<CancellationToken, Task<NotificationResult>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationResult(Success: false, Error: new ErrorInfo("SEND_FAILED", "Send failed")));
        _mockStorageAccessor
            .Setup(s => s.UpdateAsync(It.IsAny<NotificationRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedRecord);

        // Act
        var result = await _sut.ProcessAndSendNotificationAsync(message, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        _mockKafkaProducer.Verify(
            k => k.PublishNotificationRecordAsync(
                It.Is<NotificationRecord>(r => r.Status == NotificationStatus.Failed),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessAndSendNotificationAsync_KafkaPublishFails_DoesNotFailMainFlow()
    {
        // Arrange
        var message = CreateTestMessage();
        var createdRecord = CreateTestRecord() with { Status = NotificationStatus.Pending };
        var sentRecord = createdRecord with { Status = NotificationStatus.Sent, SentAt = DateTime.UtcNow };
        var emailRequest = new EmailRequest("test@example.com", "Subject", "Body");

        _mockEngine.Setup(e => e.ValidateMessage(message)).Returns(true);
        _mockStorageAccessor
            .Setup(s => s.GetByMessageIdAsync(message.MessageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationRecord?)null);
        _mockStorageAccessor
            .Setup(s => s.CreateAsync(It.IsAny<NotificationRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdRecord);
        _mockEngine.Setup(e => e.BuildEmailRequest(message)).Returns(emailRequest);
        _mockRetryPolicy
            .Setup(r => r.ExecuteWithRetryAsync(It.IsAny<Func<CancellationToken, Task<NotificationResult>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationResult(Success: true));
        _mockStorageAccessor
            .Setup(s => s.UpdateAsync(It.IsAny<NotificationRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sentRecord);
        _mockKafkaProducer
            .Setup(k => k.PublishNotificationRecordAsync(It.IsAny<NotificationRecord>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Kafka unavailable"));

        // Act
        var result = await _sut.ProcessAndSendNotificationAsync(message, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue("Main flow should succeed even if Kafka publish fails");
        result.Record.Should().NotBeNull();
        result.Record!.Status.Should().Be(NotificationStatus.Sent);
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to publish notification record to Kafka")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RetryFailedNotificationAsync_SuccessfulRetry_PublishesToKafka()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        var existingRecord = CreateTestRecord() with
        {
            Id = notificationId,
            Status = NotificationStatus.Failed,
            RetryCount = 0
        };
        var retryingRecord = existingRecord with
        {
            Status = NotificationStatus.Retrying,
            RetryCount = 1
        };
        var sentRecord = retryingRecord with
        {
            Status = NotificationStatus.Sent,
            SentAt = DateTime.UtcNow
        };

        _mockStorageAccessor
            .Setup(s => s.GetByIdAsync(notificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRecord);
        _mockStorageAccessor
            .Setup(s => s.UpdateAsync(It.IsAny<NotificationRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationRecord r, CancellationToken _) => r);
        _mockEmailAccessor
            .Setup(e => e.SendEmailAsync(It.IsAny<EmailRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationResult(Success: true));

        // Act
        var result = await _sut.RetryFailedNotificationAsync(notificationId, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _mockKafkaProducer.Verify(
            k => k.PublishNotificationRecordAsync(
                It.Is<NotificationRecord>(r => r.Status == NotificationStatus.Sent),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RetryFailedNotificationAsync_FailedRetry_PublishesFailedRecordToKafka()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        var existingRecord = CreateTestRecord() with
        {
            Id = notificationId,
            Status = NotificationStatus.Failed,
            RetryCount = 0
        };
        var retryingRecord = existingRecord with
        {
            Status = NotificationStatus.Retrying,
            RetryCount = 1
        };
        var failedRecord = retryingRecord with
        {
            Status = NotificationStatus.Failed,
            ErrorMessage = "Retry failed"
        };

        _mockStorageAccessor
            .Setup(s => s.GetByIdAsync(notificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRecord);
        _mockStorageAccessor
            .Setup(s => s.UpdateAsync(It.IsAny<NotificationRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationRecord r, CancellationToken _) => r);
        _mockEmailAccessor
            .Setup(e => e.SendEmailAsync(It.IsAny<EmailRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationResult(Success: false, Error: new ErrorInfo("RETRY_FAILED", "Retry failed")));

        // Act
        var result = await _sut.RetryFailedNotificationAsync(notificationId, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        _mockKafkaProducer.Verify(
            k => k.PublishNotificationRecordAsync(
                It.Is<NotificationRecord>(r => r.Status == NotificationStatus.Failed),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RetryFailedNotificationAsync_AlreadySent_DoesNotPublishAgain()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        var sentRecord = CreateTestRecord() with
        {
            Id = notificationId,
            Status = NotificationStatus.Sent,
            SentAt = DateTime.UtcNow
        };

        _mockStorageAccessor
            .Setup(s => s.GetByIdAsync(notificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sentRecord);

        // Act
        var result = await _sut.RetryFailedNotificationAsync(notificationId, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _mockKafkaProducer.Verify(
            k => k.PublishNotificationRecordAsync(It.IsAny<NotificationRecord>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RetryFailedNotificationAsync_NotFound_ReturnsError()
    {
        // Arrange
        var notificationId = Guid.NewGuid();

        _mockStorageAccessor
            .Setup(s => s.GetByIdAsync(notificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationRecord?)null);

        // Act
        var result = await _sut.RetryFailedNotificationAsync(notificationId, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("NOT_FOUND");
        _mockKafkaProducer.Verify(
            k => k.PublishNotificationRecordAsync(It.IsAny<NotificationRecord>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static NotificationMessage CreateTestMessage()
    {
        return new NotificationMessage(
            MessageId: "test-message-123",
            Topic: "weather-alerts",
            Subject: "Test Subject",
            Body: "Test body",
            Recipient: "test@example.com",
            Timestamp: DateTime.UtcNow
        );
    }

    private static NotificationRecord CreateTestRecord()
    {
        return new NotificationRecord(
            Id: Guid.NewGuid(),
            MessageId: "test-message-123",
            Topic: "weather-alerts",
            Subject: "Test Subject",
            Body: "Test body",
            Recipient: "test@example.com",
            Status: NotificationStatus.Sent,
            CreatedAt: DateTime.UtcNow,
            SentAt: DateTime.UtcNow,
            RetryCount: 0,
            ErrorMessage: null
        );
    }
}
