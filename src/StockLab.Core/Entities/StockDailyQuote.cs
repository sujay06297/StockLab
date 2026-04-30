namespace StockLab.Core.Entities;

public class StockDailyQuote
{
    /// <summary>
    /// 系統內部流水號。
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 交易日期。
    /// </summary>
    public DateOnly TradeDate { get; set; }

    /// <summary>
    /// 股票代號。
    /// </summary>
    public string StockCode { get; set; } = string.Empty;

    /// <summary>
    /// 股票名稱。
    /// </summary>
    public string StockName { get; set; } = string.Empty;

    /// <summary>
    /// 成交股數。
    /// </summary>
    public long? TradeVolume { get; set; }

    /// <summary>
    /// 成交金額。
    /// </summary>
    public decimal? TradeValue { get; set; }

    /// <summary>
    /// 開盤價。
    /// </summary>
    public decimal? OpeningPrice { get; set; }

    /// <summary>
    /// 最高價。
    /// </summary>
    public decimal? HighestPrice { get; set; }

    /// <summary>
    /// 最低價。
    /// </summary>
    public decimal? LowestPrice { get; set; }

    /// <summary>
    /// 收盤價。
    /// </summary>
    public decimal? ClosingPrice { get; set; }

    /// <summary>
    /// 漲跌價差的數值。
    /// </summary>
    public decimal? PriceChange { get; set; }

    /// <summary>
    /// 來源資料中的漲跌價差原始文字。
    /// </summary>
    public string PriceChangeText { get; set; } = string.Empty;

    /// <summary>
    /// 成交筆數。
    /// </summary>
    public long? TransactionCount { get; set; }

    /// <summary>
    /// 資料同步完成的 UTC 時間。
    /// </summary>
    public DateTimeOffset SyncedAtUtc { get; set; }
}
