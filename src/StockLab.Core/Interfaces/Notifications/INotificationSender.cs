namespace StockLab.Core.Interfaces.Notifications;

public interface INotificationSender
{
    Task SendAsync(string message, CancellationToken cancellationToken = default);
}
