using System.Globalization;
using System.Text.Json.Serialization;
using StockLab.Core.Entities;

namespace StockLab.Core.DTOs;

public class TwseStockDayAllDto
{
    [JsonPropertyName("Code")]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("Name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("TradeVolume")]
    public string TradeVolume { get; init; } = string.Empty;

    [JsonPropertyName("TradeValue")]
    public string TradeValue { get; init; } = string.Empty;

    [JsonPropertyName("OpeningPrice")]
    public string OpeningPrice { get; init; } = string.Empty;

    [JsonPropertyName("HighestPrice")]
    public string HighestPrice { get; init; } = string.Empty;

    [JsonPropertyName("LowestPrice")]
    public string LowestPrice { get; init; } = string.Empty;

    [JsonPropertyName("ClosingPrice")]
    public string ClosingPrice { get; init; } = string.Empty;

    [JsonPropertyName("Change")]
    public string Change { get; init; } = string.Empty;

    [JsonPropertyName("Transaction")]
    public string Transaction { get; init; } = string.Empty;

    public StockDailyQuote ToEntity(DateOnly tradeDate)
    {
        return new StockDailyQuote
        {
            TradeDate = tradeDate,
            StockCode = Code.Trim(),
            StockName = Name.Trim(),
            TradeVolume = ParseLongOrNull(TradeVolume),
            TradeValue = ParseDecimalOrNull(TradeValue),
            OpeningPrice = ParseDecimalOrNull(OpeningPrice),
            HighestPrice = ParseDecimalOrNull(HighestPrice),
            LowestPrice = ParseDecimalOrNull(LowestPrice),
            ClosingPrice = ParseDecimalOrNull(ClosingPrice),
            PriceChange = ParseSignedDecimalOrNull(Change),
            PriceChangeText = Change.Trim(),
            TransactionCount = ParseLongOrNull(Transaction)
        };
    }

    private static long? ParseLongOrNull(string value)
    {
        var normalized = NormalizeNumericText(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return long.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static decimal? ParseDecimalOrNull(string value)
    {
        var normalized = NormalizeNumericText(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static decimal? ParseSignedDecimalOrNull(string value)
    {
        var normalized = NormalizeNumericText(value)
            .Replace("+", string.Empty, StringComparison.Ordinal)
            .Replace("\u2212", "-", StringComparison.Ordinal)
            .Replace("\uFF0D", "-", StringComparison.Ordinal);

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return decimal.TryParse(normalized, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static string NormalizeNumericText(string value)
    {
        return value
            .Trim()
            .Replace(",", string.Empty, StringComparison.Ordinal)
            .Replace("--", string.Empty, StringComparison.Ordinal);
    }
}
