using StockLab.Core.Entities;

namespace StockLab.Core.Interfaces.Repositories;

public interface IStockDailyQuoteRepository
{
    Task UpsertRangeAsync(
        DateOnly tradeDate,
        IReadOnlyCollection<StockDailyQuote> quotes,
        CancellationToken cancellationToken = default);

    Task<StockDailyQuote?> GetByStockCodeAndTradeDateAsync(
        string stockCode,
        DateOnly tradeDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<StockDailyQuote>> GetByTradeDateAsync(
        DateOnly tradeDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<StockDailyQuote>> GetByStockCodeAsync(
        string stockCode,
        DateOnly? fromTradeDate = null,
        DateOnly? toTradeDate = null,
        CancellationToken cancellationToken = default);

    Task<DateOnly?> GetLatestTradeDateAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<StockDailyQuote>> GetRecentQuotesAsync(
        DateOnly toTradeDate,
        int tradeDateCount,
        CancellationToken cancellationToken = default);
}
