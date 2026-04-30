namespace StockLab.Core.Models;

public class StockSelectionCandidate
{
    public string StockCode { get; init; } = string.Empty;

    public string StockName { get; init; } = string.Empty;

    public DateOnly TradeDate { get; init; }

    public decimal ClosingPrice { get; init; }

    public decimal PriceChange { get; init; }

    public decimal PriceChangePercent { get; init; }

    public decimal TradeValue { get; init; }

    public decimal VolumeRatio { get; init; }

    public decimal AverageClose5 { get; init; }

    public decimal AverageClose10 { get; init; }

    public decimal Score { get; init; }

    public string Reason { get; init; } = string.Empty;
}
