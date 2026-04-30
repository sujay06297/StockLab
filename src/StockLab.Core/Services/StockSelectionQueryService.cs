using StockLab.Core.Interfaces.Repositories;
using StockLab.Core.Interfaces.Services;
using StockLab.Core.Models;

namespace StockLab.Core.Services;

public class StockSelectionQueryService(IStockSelectionResultRepository stockSelectionResultRepository) : IStockSelectionQueryService
{
    private const int DefaultHistoryLimit = 20;
    private const int MinimumHistoryLimit = 1;
    private const int MaximumHistoryLimit = 100;

    private readonly IStockSelectionResultRepository _stockSelectionResultRepository = stockSelectionResultRepository;

    public async Task<StockSelectionQueryResult?> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        var latestTradeDate = await _stockSelectionResultRepository.GetLatestResultTradeDateAsync(
            StockSelectionService.MomentumStrategyName,
            cancellationToken);

        return latestTradeDate is null
            ? null
            : await GetByTradeDateAsync(latestTradeDate.Value, cancellationToken);
    }

    public async Task<StockSelectionQueryResult?> GetByTradeDateAsync(
        DateOnly tradeDate,
        CancellationToken cancellationToken = default)
    {
        var hasRun = await _stockSelectionResultRepository.HasStrategyRunAsync(
            tradeDate,
            StockSelectionService.MomentumStrategyName,
            cancellationToken);
        if (!hasRun)
        {
            return null;
        }

        var results = await _stockSelectionResultRepository.GetStrategyResultsAsync(
            tradeDate,
            StockSelectionService.MomentumStrategyName,
            cancellationToken);

        return new StockSelectionQueryResult
        {
            StrategyName = StockSelectionService.MomentumStrategyName,
            TradeDate = tradeDate,
            CandidateCount = results.Count,
            Candidates = results
        };
    }

    public async Task<StockSelectionHistoryResult> GetHistoryAsync(
        int limit = DefaultHistoryLimit,
        CancellationToken cancellationToken = default)
    {
        var normalizedLimit = Math.Clamp(limit, MinimumHistoryLimit, MaximumHistoryLimit);
        var tradeDates = await _stockSelectionResultRepository.GetResultTradeDatesAsync(
            StockSelectionService.MomentumStrategyName,
            normalizedLimit,
            cancellationToken);

        return new StockSelectionHistoryResult
        {
            StrategyName = StockSelectionService.MomentumStrategyName,
            Limit = normalizedLimit,
            TradeDates = tradeDates
        };
    }
}
