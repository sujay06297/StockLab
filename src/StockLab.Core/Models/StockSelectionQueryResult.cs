using StockLab.Core.Entities;

namespace StockLab.Core.Models;

public class StockSelectionQueryResult
{
    public string StrategyName { get; init; } = string.Empty;

    public DateOnly TradeDate { get; init; }

    public int CandidateCount { get; init; }

    public IReadOnlyCollection<StockSelectionResult> Candidates { get; init; } = Array.Empty<StockSelectionResult>();
}
