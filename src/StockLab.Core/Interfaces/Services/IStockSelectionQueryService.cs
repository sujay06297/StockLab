using StockLab.Core.Models;

namespace StockLab.Core.Interfaces.Services;

public interface IStockSelectionQueryService
{
    Task<StockSelectionQueryResult?> GetLatestAsync(CancellationToken cancellationToken = default);

    Task<StockSelectionQueryResult?> GetByTradeDateAsync(
        DateOnly tradeDate,
        CancellationToken cancellationToken = default);

    Task<StockSelectionHistoryResult> GetHistoryAsync(
        int limit = 20,
        CancellationToken cancellationToken = default);
}
