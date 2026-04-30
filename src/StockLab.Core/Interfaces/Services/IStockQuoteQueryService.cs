using StockLab.Core.Entities;

namespace StockLab.Core.Interfaces.Services;

public interface IStockQuoteQueryService
{
    Task<StockDailyQuote?> GetLatestQuoteAsync(
        string stockCode,
        CancellationToken cancellationToken = default);
}
