namespace NotificationService.Contracts;

public interface IEmailAccessor
{
    Task<NotificationResult> SendEmailAsync(EmailRequest request, CancellationToken cancellationToken = default);
}
