namespace NotificationService.Contracts;

public record NotificationResult(
    bool Success,
    string? MessageId = null,
    ErrorInfo? Error = null
);

public record ProcessMessageResult(
    bool Success,
    NotificationRecord? Record = null,
    ErrorInfo? Error = null
);
