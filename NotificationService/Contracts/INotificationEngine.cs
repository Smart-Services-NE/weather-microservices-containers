namespace NotificationService.Contracts;

public interface INotificationEngine
{
    NotificationMessage ParseMessage(string topic, string messageContent);
    EmailRequest BuildEmailRequest(NotificationMessage message);
    bool ValidateMessage(NotificationMessage message);
}
