using StockLab.Core.Entities;
using StockLab.Core.Interfaces.Repositories;
using StockLab.Infrastructure.Data;
using Dapper;
using System.Globalization;

namespace StockLab.Infrastructure.Repositories;

public class StockDailyQuoteRepository(IStockDbConnectionFactory connectionFactory) : IStockDailyQuoteRepository
{
    private readonly IStockDbConnectionFactory _connectionFactory = connectionFactory;

    public async Task UpsertRangeAsync(
        DateOnly tradeDate,
        IReadOnlyCollection<StockDailyQuote> quotes,
        CancellationToken cancellationToken = default)
    {
        if (quotes.Count == 0)
        {
            return;
        }

        var syncedAt = DateTimeOffset.UtcNow;
        var rows = quotes.Select(quote =>
        {
            quote.SyncedAtUtc = syncedAt;
            return new
            {
                TradeDate = quote.TradeDate.ToString("yyyy-MM-dd"),
                quote.StockCode,
                quote.StockName,
                quote.TradeVolume,
                quote.TradeValue,
                quote.OpeningPrice,
                quote.HighestPrice,
                quote.LowestPrice,
                quote.ClosingPrice,
                quote.PriceChange,
                quote.PriceChangeText,
                TransactionCount = quote.TransactionCount,
                SyncedAtUtc = quote.SyncedAtUtc.UtcDateTime
            };
        }).ToArray();

        const string sql = """
            INSERT INTO StockDailyQuotes (
                TradeDate,
                StockCode,
                StockName,
                TradeVolume,
                TradeValue,
                OpeningPrice,
                HighestPrice,
                LowestPrice,
                ClosingPrice,
                PriceChange,
                PriceChangeText,
                TransactionCount,
                SyncedAtUtc
            )
            VALUES (
                @TradeDate,
                @StockCode,
                @StockName,
                @TradeVolume,
                @TradeValue,
                @OpeningPrice,
                @HighestPrice,
                @LowestPrice,
                @ClosingPrice,
                @PriceChange,
                @PriceChangeText,
                @TransactionCount,
                @SyncedAtUtc
            )
            ON DUPLICATE KEY UPDATE
                StockName = VALUES(StockName),
                TradeVolume = VALUES(TradeVolume),
                TradeValue = VALUES(TradeValue),
                OpeningPrice = VALUES(OpeningPrice),
                HighestPrice = VALUES(HighestPrice),
                LowestPrice = VALUES(LowestPrice),
                ClosingPrice = VALUES(ClosingPrice),
                PriceChange = VALUES(PriceChange),
                PriceChangeText = VALUES(PriceChangeText),
                TransactionCount = VALUES(TransactionCount),
                SyncedAtUtc = VALUES(SyncedAtUtc);
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, rows, cancellationToken: cancellationToken));
    }

    public async Task<StockDailyQuote?> GetByStockCodeAndTradeDateAsync(
        string stockCode,
        DateOnly tradeDate,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                Id,
                TradeDate,
                StockCode,
                StockName,
                TradeVolume,
                TradeValue,
                OpeningPrice,
                HighestPrice,
                LowestPrice,
                ClosingPrice,
                PriceChange,
                PriceChangeText,
                TransactionCount,
                SyncedAtUtc
            FROM StockDailyQuotes
            WHERE StockCode = @StockCode
              AND TradeDate = @TradeDate
            LIMIT 1;
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<StockDailyQuoteRow>(
            new CommandDefinition(
                sql,
                new
                {
                    StockCode = stockCode.Trim(),
                    TradeDate = FormatTradeDate(tradeDate)
                },
                cancellationToken: cancellationToken));

        return row?.ToEntity();
    }

    public async Task<IReadOnlyCollection<StockDailyQuote>> GetByTradeDateAsync(
        DateOnly tradeDate,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                Id,
                TradeDate,
                StockCode,
                StockName,
                TradeVolume,
                TradeValue,
                OpeningPrice,
                HighestPrice,
                LowestPrice,
                ClosingPrice,
                PriceChange,
                PriceChangeText,
                TransactionCount,
                SyncedAtUtc
            FROM StockDailyQuotes
            WHERE TradeDate = @TradeDate
            ORDER BY StockCode;
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var rows = await connection.QueryAsync<StockDailyQuoteRow>(
            new CommandDefinition(
                sql,
                new { TradeDate = FormatTradeDate(tradeDate) },
                cancellationToken: cancellationToken));

        return rows.Select(row => row.ToEntity()).ToArray();
    }

    public async Task<IReadOnlyCollection<StockDailyQuote>> GetByStockCodeAsync(
        string stockCode,
        DateOnly? fromTradeDate = null,
        DateOnly? toTradeDate = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                Id,
                TradeDate,
                StockCode,
                StockName,
                TradeVolume,
                TradeValue,
                OpeningPrice,
                HighestPrice,
                LowestPrice,
                ClosingPrice,
                PriceChange,
                PriceChangeText,
                TransactionCount,
                SyncedAtUtc
            FROM StockDailyQuotes
            WHERE StockCode = @StockCode
              AND (@FromTradeDate IS NULL OR TradeDate >= @FromTradeDate)
              AND (@ToTradeDate IS NULL OR TradeDate <= @ToTradeDate)
            ORDER BY TradeDate DESC;
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var rows = await connection.QueryAsync<StockDailyQuoteRow>(
            new CommandDefinition(
                sql,
                new
                {
                    StockCode = stockCode.Trim(),
                    FromTradeDate = fromTradeDate is null ? null : FormatTradeDate(fromTradeDate.Value),
                    ToTradeDate = toTradeDate is null ? null : FormatTradeDate(toTradeDate.Value)
                },
                cancellationToken: cancellationToken));

        return rows.Select(row => row.ToEntity()).ToArray();
    }

    private static string FormatTradeDate(DateOnly tradeDate)
    {
        return tradeDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private sealed class StockDailyQuoteRow
    {
        public int Id { get; set; }

        public DateTime TradeDate { get; set; }

        public string StockCode { get; set; } = string.Empty;

        public string StockName { get; set; } = string.Empty;

        public long? TradeVolume { get; set; }

        public decimal? TradeValue { get; set; }

        public decimal? OpeningPrice { get; set; }

        public decimal? HighestPrice { get; set; }

        public decimal? LowestPrice { get; set; }

        public decimal? ClosingPrice { get; set; }

        public decimal? PriceChange { get; set; }

        public string PriceChangeText { get; set; } = string.Empty;

        public long? TransactionCount { get; set; }

        public DateTime SyncedAtUtc { get; set; }

        public StockDailyQuote ToEntity()
        {
            return new StockDailyQuote
            {
                Id = Id,
                TradeDate = DateOnly.FromDateTime(TradeDate),
                StockCode = StockCode,
                StockName = StockName,
                TradeVolume = TradeVolume,
                TradeValue = TradeValue,
                OpeningPrice = OpeningPrice,
                HighestPrice = HighestPrice,
                LowestPrice = LowestPrice,
                ClosingPrice = ClosingPrice,
                PriceChange = PriceChange,
                PriceChangeText = PriceChangeText,
                TransactionCount = TransactionCount,
                SyncedAtUtc = new DateTimeOffset(DateTime.SpecifyKind(SyncedAtUtc, DateTimeKind.Utc))
            };
        }
    }
}
