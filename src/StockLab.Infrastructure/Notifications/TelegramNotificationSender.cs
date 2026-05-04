using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockLab.Infrastructure.Options;

namespace StockLab.Infrastructure.Notifications;

public class TelegramNotificationSender(
    IHttpClientFactory httpClientFactory,
    IOptions<TelegramOptions> options,
    ILogger<TelegramNotificationSender> logger) : INotificationChannelSender
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly TelegramOptions _options = options.Value;
    private readonly ILogger<TelegramNotificationSender> _logger = logger;

    public async Task SendAsync(string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BotToken) || string.IsNullOrWhiteSpace(_options.ChatId))
        {
            _logger.LogWarning("Telegram 通知未送出：缺少 Telegram:BotToken 或 Telegram:ChatId 設定。");
            return;
        }

        using var httpClient = _httpClientFactory.CreateClient("telegram");
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["chat_id"] = _options.ChatId,
            ["text"] = message
        });

        var requestUri = $"bot{_options.BotToken}/sendMessage";
        using var response = await httpClient.PostAsync(requestUri, content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
