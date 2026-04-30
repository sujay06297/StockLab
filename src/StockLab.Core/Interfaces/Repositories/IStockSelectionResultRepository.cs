using StockLab.Core.Entities;

namespace StockLab.Core.Interfaces.Repositories;

public interface IStockSelectionResultRepository
{
    Task<bool> HasStrategyRunAsync(
        DateOnly tradeDate,
        string strategyName,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<StockSelectionResult>> GetStrategyResultsAsync(
        DateOnly tradeDate,
        string strategyName,
        CancellationToken cancellationToken = default);

    Task ReplaceStrategyResultsAsync(
        DateOnly tradeDate,
        string strategyName,
        IReadOnlyCollection<StockSelectionResult> results,
        CancellationToken cancellationToken = default);
}
