namespace StockLab.Infrastructure.Notifications;

public interface INotificationChannelSender
{
    Task SendAsync(string message, CancellationToken cancellationToken = default);
}
