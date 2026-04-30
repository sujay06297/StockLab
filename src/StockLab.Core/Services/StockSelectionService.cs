using StockLab.Core.Entities;
using StockLab.Core.Interfaces.Repositories;
using StockLab.Core.Interfaces.Services;
using StockLab.Core.Models;

namespace StockLab.Core.Services;

public class StockSelectionService(
    IStockDailyQuoteRepository stockDailyQuoteRepository,
    IStockSelectionResultRepository stockSelectionResultRepository) : IStockSelectionService
{
    public const string MomentumStrategyName = "MomentumLiquidityTrend";

    private const int LookbackTradeDateCount = 20;
    private const int MinimumHistoryCount = 10;
    private const int MaxCandidateCount = 10;
    private const decimal MinimumClosingPrice = 10m;
    private const decimal MinimumTradeValue = 50_000_000m;
    private const decimal MinimumClosePosition = 0.7m;
    private const decimal MinimumVolumeRatio = 1.2m;

    private readonly IStockDailyQuoteRepository _stockDailyQuoteRepository = stockDailyQuoteRepository;
    private readonly IStockSelectionResultRepository _stockSelectionResultRepository = stockSelectionResultRepository;

    public async Task<IReadOnlyCollection<StockSelectionCandidate>> SelectMomentumCandidatesAsync(
        CancellationToken cancellationToken = default)
    {
        var latestTradeDate = await _stockDailyQuoteRepository.GetLatestTradeDateAsync(cancellationToken);
        if (latestTradeDate is null)
        {
            return Array.Empty<StockSelectionCandidate>();
        }

        var quotes = await _stockDailyQuoteRepository.GetRecentQuotesAsync(
            latestTradeDate.Value,
            LookbackTradeDateCount,
            cancellationToken);

        var candidates = quotes
            .GroupBy(quote => quote.StockCode)
            .Select(group => BuildCandidate(group.OrderByDescending(quote => quote.TradeDate).ToArray(), latestTradeDate.Value))
            .Where(candidate => candidate is not null)
            .Select(candidate => candidate!)
            .OrderByDescending(candidate => candidate.Score)
            .ThenByDescending(candidate => candidate.TradeValue)
            .Take(MaxCandidateCount)
            .ToArray();

        await _stockSelectionResultRepository.ReplaceStrategyResultsAsync(
            latestTradeDate.Value,
            MomentumStrategyName,
            candidates.Select(ToResult).ToArray(),
            cancellationToken);

        return candidates;
    }

    private static StockSelectionResult ToResult(StockSelectionCandidate candidate, int index)
    {
        return new StockSelectionResult
        {
            TradeDate = candidate.TradeDate,
            StrategyName = MomentumStrategyName,
            Rank = index + 1,
            StockCode = candidate.StockCode,
            StockName = candidate.StockName,
            ClosingPrice = candidate.ClosingPrice,
            PriceChange = candidate.PriceChange,
            PriceChangePercent = candidate.PriceChangePercent,
            TradeValue = candidate.TradeValue,
            VolumeRatio = candidate.VolumeRatio,
            AverageClose5 = candidate.AverageClose5,
            AverageClose10 = candidate.AverageClose10,
            Score = candidate.Score,
            Reason = candidate.Reason,
            SelectedAtUtc = DateTimeOffset.UtcNow
        };
    }

    private static StockSelectionCandidate? BuildCandidate(
        IReadOnlyList<StockDailyQuote> quotes,
        DateOnly latestTradeDate)
    {
        var current = quotes.FirstOrDefault(quote => quote.TradeDate == latestTradeDate);
        if (current is null || quotes.Count < MinimumHistoryCount)
        {
            return null;
        }

        if (!HasRequiredCurrentValues(current))
        {
            return null;
        }

        var closingPrice = current.ClosingPrice!.Value;
        var openingPrice = current.OpeningPrice!.Value;
        var highestPrice = current.HighestPrice!.Value;
        var lowestPrice = current.LowestPrice!.Value;
        var priceChange = current.PriceChange!.Value;
        var tradeValue = current.TradeValue!.Value;
        var tradeVolume = current.TradeVolume!.Value;

        var previousClose = closingPrice - priceChange;
        if (previousClose <= 0)
        {
            return null;
        }

        var averageClose5 = AverageClose(quotes, 5);
        var averageClose10 = AverageClose(quotes, 10);
        var averageVolume10 = AverageVolume(quotes.Skip(1), 10);
        if (averageClose5 is null || averageClose10 is null || averageVolume10 is null || averageVolume10 <= 0)
        {
            return null;
        }

        var closePosition = highestPrice == lowestPrice
            ? 1m
            : (closingPrice - lowestPrice) / (highestPrice - lowestPrice);
        var priceChangePercent = priceChange / previousClose * 100m;
        var volumeRatio = tradeVolume / averageVolume10.Value;
        var movingAverageSpread = (averageClose5.Value - averageClose10.Value) / averageClose10.Value * 100m;

        if (closingPrice < MinimumClosingPrice || tradeValue < MinimumTradeValue)
        {
            return null;
        }

        if (priceChange <= 0 || closingPrice <= openingPrice)
        {
            return null;
        }

        if (closingPrice <= averageClose5 || averageClose5 <= averageClose10)
        {
            return null;
        }

        if (closePosition < MinimumClosePosition || volumeRatio < MinimumVolumeRatio)
        {
            return null;
        }

        var score = priceChangePercent * 2m
            + Math.Min(volumeRatio, 3m) * 1.5m
            + closePosition * 2m
            + movingAverageSpread;

        return new StockSelectionCandidate
        {
            StockCode = current.StockCode,
            StockName = current.StockName,
            TradeDate = current.TradeDate,
            ClosingPrice = closingPrice,
            PriceChange = priceChange,
            PriceChangePercent = Math.Round(priceChangePercent, 2),
            TradeValue = tradeValue,
            VolumeRatio = Math.Round(volumeRatio, 2),
            AverageClose5 = Math.Round(averageClose5.Value, 2),
            AverageClose10 = Math.Round(averageClose10.Value, 2),
            Score = Math.Round(score, 2),
            Reason = "收盤站上 5 日與 10 日均線、5 日均線高於 10 日均線、成交量放大且收盤接近當日高點"
        };
    }

    private static bool HasRequiredCurrentValues(StockDailyQuote quote)
    {
        return quote.TradeVolume is not null
            && quote.TradeValue is not null
            && quote.OpeningPrice is not null
            && quote.HighestPrice is not null
            && quote.LowestPrice is not null
            && quote.ClosingPrice is not null
            && quote.PriceChange is not null;
    }

    private static decimal? AverageClose(IEnumerable<StockDailyQuote> quotes, int count)
    {
        var values = quotes
            .Where(quote => quote.ClosingPrice is not null)
            .Take(count)
            .Select(quote => quote.ClosingPrice!.Value)
            .ToArray();

        return values.Length == count ? values.Average() : null;
    }

    private static decimal? AverageVolume(IEnumerable<StockDailyQuote> quotes, int count)
    {
        var values = quotes
            .Where(quote => quote.TradeVolume is not null)
            .Take(count)
            .Select(quote => (decimal)quote.TradeVolume!.Value)
            .ToArray();

        return values.Length == count ? values.Average() : null;
    }
}
