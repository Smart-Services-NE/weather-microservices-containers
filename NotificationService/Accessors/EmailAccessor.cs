using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NotificationService.Contracts;

namespace NotificationService.Accessors;

public class EmailAccessor : IEmailAccessor
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailAccessor> _logger;
    private readonly string _defaultFromAddress;
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string? _smtpUsername;
    private readonly string? _smtpPassword;
    private readonly bool _enableSsl;

    public EmailAccessor(IConfiguration configuration, ILogger<EmailAccessor> logger)
    {
        _configuration = configuration;
        _logger = logger;

        _smtpHost = configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
        _smtpPort = int.Parse(configuration["Email:SmtpPort"] ?? "587");
        _smtpUsername = configuration["Email:SmtpUsername"];
        _smtpPassword = configuration["Email:SmtpPassword"];
        _enableSsl = bool.Parse(configuration["Email:EnableSsl"] ?? "true");
        _defaultFromAddress = configuration["Email:DefaultFrom"] ?? "noreply@example.com";
    }

    public async Task<NotificationResult> SendEmailAsync(
        EmailRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var mailMessage = new MailMessage
            {
                From = new MailAddress(request.From ?? _defaultFromAddress),
                Subject = request.Subject,
                Body = request.Body,
                IsBodyHtml = request.IsHtml
            };

            mailMessage.To.Add(request.To);

            using var smtpClient = new SmtpClient(_smtpHost, _smtpPort)
            {
                EnableSsl = _enableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            if (!string.IsNullOrEmpty(_smtpUsername) && !string.IsNullOrEmpty(_smtpPassword))
            {
                smtpClient.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
            }

            await smtpClient.SendMailAsync(mailMessage, cancellationToken);

            _logger.LogInformation("Email sent successfully to {Recipient}", request.To);

            return new NotificationResult(
                Success: true,
                MessageId: Guid.NewGuid().ToString()
            );
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "SMTP error sending email to {Recipient}", request.To);

            return new NotificationResult(
                Success: false,
                Error: new ErrorInfo("SMTP_ERROR", $"Failed to send email: {ex.Message}")
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending email to {Recipient}", request.To);

            return new NotificationResult(
                Success: false,
                Error: new ErrorInfo("EMAIL_ERROR", $"Unexpected error: {ex.Message}")
            );
        }
    }
}
