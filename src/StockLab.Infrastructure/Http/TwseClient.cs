using System.Text.Json;
using Microsoft.Extensions.Logging;
using StockLab.Core.DTOs;
using StockLab.Core.Interfaces.Clients;

namespace StockLab.Infrastructure.Http;

public class TwseClient(
    IHttpClientFactory httpClientFactory,
    ILogger<TwseClient> logger) : ITwseClient
{
    private const string Url = "https://openapi.twse.com.tw/v1/exchangeReport/STOCK_DAY_ALL";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ILogger<TwseClient> _logger = logger;

    public async Task<IReadOnlyCollection<TwseStockDayAllDto>> GetStockDayAllAsync(CancellationToken cancellationToken = default)
    {
        using var httpClient = _httpClientFactory.CreateClient("twse");
        httpClient.Timeout = TimeSpan.FromSeconds(30);

        _logger.LogInformation("正在從證交所取得每日股票行情資料，來源網址：{Url}。", Url);

        using var response = await httpClient.GetAsync(Url, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var rows = await JsonSerializer.DeserializeAsync<List<TwseStockDayAllDto>>(contentStream, JsonOptions, cancellationToken);

        return rows ?? (IReadOnlyCollection<TwseStockDayAllDto>)Array.Empty<TwseStockDayAllDto>();
    }
}
