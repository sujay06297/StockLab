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
    private const int MessageChunkSize = 1800;

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

        if (string.IsNullOrWhiteSpace(message))
        {
            _logger.LogWarning("Discord 通知未送出：訊息內容為空。");
            return;
        }

        using var httpClient = _httpClientFactory.CreateClient("discord");
        foreach (var chunk in SplitMessage(message))
        {
            using var content = CreateJsonContent(chunk);
            using var response = await httpClient.PostAsync(_options.WebhookUrl, content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Discord 通知送出失敗：HTTP {StatusCode} {ReasonPhrase}，回應內容：{ResponseBody}",
                    (int)response.StatusCode,
                    response.ReasonPhrase,
                    responseBody);

                response.EnsureSuccessStatusCode();
            }
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
        var normalizedMessage = message.Trim();
        if (normalizedMessage.Length <= DiscordContentLimit)
        {
            yield return normalizedMessage;
            yield break;
        }

        for (var index = 0; index < normalizedMessage.Length; index += MessageChunkSize)
        {
            var chunk = normalizedMessage[index..Math.Min(index + MessageChunkSize, normalizedMessage.Length)].Trim();
            if (!string.IsNullOrWhiteSpace(chunk))
            {
                yield return chunk;
            }
        }
    }
}
