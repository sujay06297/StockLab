namespace StockLab.Core.Entities;

public class StockSelectionResult
{
    /// <summary>
    /// 系統內部流水號。
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 選股所依據的交易日期。
    /// </summary>
    public DateOnly TradeDate { get; set; }

    /// <summary>
    /// 選股策略名稱。
    /// </summary>
    public string StrategyName { get; set; } = string.Empty;

    /// <summary>
    /// 該策略在同一交易日產出的排序名次。
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// 股票代號。
    /// </summary>
    public string StockCode { get; set; } = string.Empty;

    /// <summary>
    /// 股票名稱。
    /// </summary>
    public string StockName { get; set; } = string.Empty;

    /// <summary>
    /// 選股當日收盤價。
    /// </summary>
    public decimal ClosingPrice { get; set; }

    /// <summary>
    /// 選股當日漲跌價差。
    /// </summary>
    public decimal PriceChange { get; set; }

    /// <summary>
    /// 選股當日漲跌幅百分比。
    /// </summary>
    public decimal PriceChangePercent { get; set; }

    /// <summary>
    /// 選股當日成交金額。
    /// </summary>
    public decimal TradeValue { get; set; }

    /// <summary>
    /// 選股當日成交量相對於近期均量的倍數。
    /// </summary>
    public decimal VolumeRatio { get; set; }

    /// <summary>
    /// 選股當日含當日收盤價計算出的 5 日平均收盤價。
    /// </summary>
    public decimal AverageClose5 { get; set; }

    /// <summary>
    /// 選股當日含當日收盤價計算出的 10 日平均收盤價。
    /// </summary>
    public decimal AverageClose10 { get; set; }

    /// <summary>
    /// 策略綜合評分；分數越高代表越符合策略偏好的動能與量能條件。
    /// </summary>
    public decimal Score { get; set; }

    /// <summary>
    /// 股票入選的策略理由摘要。
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// 策略產生此筆結果的 UTC 時間。
    /// </summary>
    public DateTimeOffset SelectedAtUtc { get; set; }
}
