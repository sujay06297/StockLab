using Microsoft.Extensions.Logging;
using StockLab.Core.Interfaces.Notifications;

namespace StockLab.Infrastructure.Notifications;

public class CompositeNotificationSender(
    IEnumerable<INotificationChannelSender> channelSenders,
    ILogger<CompositeNotificationSender> logger) : INotificationSender
{
    private readonly IReadOnlyCollection<INotificationChannelSender> _channelSenders = channelSenders.ToArray();
    private readonly ILogger<CompositeNotificationSender> _logger = logger;

    public async Task SendAsync(string message, CancellationToken cancellationToken = default)
    {
        foreach (var channelSender in _channelSenders)
        {
            try
            {
                await channelSender.SendAsync(message, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(
                    ex,
                    "通知通道 {ChannelSender} 送出失敗。",
                    channelSender.GetType().Name);
            }
        }
    }
}
