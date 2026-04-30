using StockLab.Core.Interfaces.Clients;
using StockLab.Core.Interfaces.Repositories;
using StockLab.Core.Interfaces.Services;

namespace StockLab.Core.Services;

public class StockSyncService(
    ITwseClient twseClient,
    IStockDailyQuoteRepository stockDailyQuoteRepository) : IStockSyncService
{
    private readonly ITwseClient _twseClient = twseClient;
    private readonly IStockDailyQuoteRepository _stockDailyQuoteRepository = stockDailyQuoteRepository;

    public async Task<int> SyncStockDayAllAsync(CancellationToken cancellationToken = default)
    {
        var tradeDate = GetTaipeiToday();
        var rows = await _twseClient.GetStockDayAllAsync(cancellationToken);

        var quotes = rows
            .Select(x => x.ToEntity(tradeDate))
            .Where(x => !string.IsNullOrWhiteSpace(x.StockCode))
            .ToArray();

        if (quotes.Length == 0)
        {
            throw new InvalidOperationException("Parsed zero valid stock rows from TWSE response.");
        }

        await _stockDailyQuoteRepository.UpsertRangeAsync(tradeDate, quotes, cancellationToken);
        return quotes.Length;
    }

    private static DateOnly GetTaipeiToday()
    {
        var taipeiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
        var taipeiNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, taipeiTimeZone);
        return DateOnly.FromDateTime(taipeiNow.DateTime);
    }
}
