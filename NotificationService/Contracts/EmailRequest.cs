namespace NotificationService.Contracts;

public record EmailRequest(
    string To,
    string Subject,
    string Body,
    string? From = null,
    bool IsHtml = true
);
