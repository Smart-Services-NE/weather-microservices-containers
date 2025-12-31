using System.Text.Json;
using NotificationService.Contracts;

namespace NotificationService.Engines;

public class NotificationEngine : INotificationEngine
{
    public NotificationMessage ParseMessage(string topic, string messageContent)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(messageContent);
            var root = jsonDoc.RootElement;

            var messageId = root.TryGetProperty("messageId", out var msgId)
                ? msgId.GetString() ?? Guid.NewGuid().ToString()
                : Guid.NewGuid().ToString();

            var subject = root.TryGetProperty("subject", out var subj)
                ? subj.GetString() ?? "Notification"
                : "Notification";

            var body = root.TryGetProperty("body", out var bdy)
                ? bdy.GetString() ?? string.Empty
                : string.Empty;

            var recipient = root.TryGetProperty("recipient", out var recip)
                ? recip.GetString() ?? string.Empty
                : string.Empty;

            var timestamp = root.TryGetProperty("timestamp", out var ts)
                ? DateTime.Parse(ts.GetString() ?? DateTime.UtcNow.ToString())
                : DateTime.UtcNow;

            Dictionary<string, string>? metadata = null;
            if (root.TryGetProperty("metadata", out var meta))
            {
                metadata = new Dictionary<string, string>();
                foreach (var prop in meta.EnumerateObject())
                {
                    metadata[prop.Name] = prop.Value.GetString() ?? string.Empty;
                }
            }

            return new NotificationMessage(
                messageId,
                topic,
                subject,
                body,
                recipient,
                timestamp,
                metadata
            );
        }
        catch (JsonException)
        {
            return new NotificationMessage(
                Guid.NewGuid().ToString(),
                topic,
                "Raw Message",
                messageContent,
                string.Empty,
                DateTime.UtcNow
            );
        }
    }

    public EmailRequest BuildEmailRequest(NotificationMessage message)
    {
        var from = message.Metadata?.GetValueOrDefault("from");
        var isHtml = message.Metadata?.GetValueOrDefault("isHtml")?.ToLowerInvariant() == "true";

        return new EmailRequest(
            message.Recipient,
            message.Subject,
            message.Body,
            from,
            isHtml
        );
    }

    public bool ValidateMessage(NotificationMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.Recipient))
            return false;

        if (string.IsNullOrWhiteSpace(message.Subject))
            return false;

        if (string.IsNullOrWhiteSpace(message.Body))
            return false;

        if (!IsValidEmail(message.Recipient))
            return false;

        return true;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
