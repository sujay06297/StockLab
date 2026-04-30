namespace StockLab.Core.Models;

public class StockSelectionHistoryResult
{
    public string StrategyName { get; init; } = string.Empty;

    public int Limit { get; init; }

    public IReadOnlyCollection<DateOnly> TradeDates { get; init; } = Array.Empty<DateOnly>();
}
