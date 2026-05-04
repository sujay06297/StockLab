using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockLab.Infrastructure.Options;

namespace StockLab.Infrastructure.Notifications;

public class DiscordNotificationSender(
    IHttpClientFactory httpClientFactory,
    IOptions<DiscordOptions> options,
    ILogger<DiscordNotificationSender> logger) : INotificationChannelSender
{
    private const int DiscordContentLimit = 2000;
    private const int MessageChunkSize = 1900;

    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly DiscordOptions _options = options.Value;
    private readonly ILogger<DiscordNotificationSender> _logger = logger;

    public async Task SendAsync(string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookUrl))
        {
            _logger.LogWarning("Discord 通知未送出：缺少 Discord:WebhookUrl 設定。");
            return;
        }

        using var httpClient = _httpClientFactory.CreateClient("discord");
        foreach (var chunk in SplitMessage(message))
        {
            using var content = CreateJsonContent(chunk);
            using var response = await httpClient.PostAsync(_options.WebhookUrl, content, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
    }

    private StringContent CreateJsonContent(string message)
    {
        var payload = string.IsNullOrWhiteSpace(_options.Username)
            ? new Dictionary<string, string> { ["content"] = message }
            : new Dictionary<string, string>
            {
                ["content"] = message,
                ["username"] = _options.Username
            };

        return new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");
    }

    private static IEnumerable<string> SplitMessage(string message)
    {
        if (message.Length <= DiscordContentLimit)
        {
            yield return message;
            yield break;
        }

        for (var index = 0; index < message.Length; index += MessageChunkSize)
        {
            yield return message[index..Math.Min(index + MessageChunkSize, message.Length)];
        }
    }
}
