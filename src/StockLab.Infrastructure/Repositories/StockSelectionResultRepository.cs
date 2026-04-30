using System.Globalization;
using Dapper;
using StockLab.Core.Entities;
using StockLab.Core.Interfaces.Repositories;
using StockLab.Infrastructure.Data;

namespace StockLab.Infrastructure.Repositories;

public class StockSelectionResultRepository(IStockDbConnectionFactory connectionFactory) : IStockSelectionResultRepository
{
    private readonly IStockDbConnectionFactory _connectionFactory = connectionFactory;

    public async Task<bool> HasStrategyRunAsync(
        DateOnly tradeDate,
        string strategyName,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM StockSelectionRuns
            WHERE TradeDate = @TradeDate
              AND StrategyName = @StrategyName;
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var count = await connection.QuerySingleAsync<int>(
            new CommandDefinition(
                sql,
                new
                {
                    TradeDate = FormatTradeDate(tradeDate),
                    StrategyName = strategyName
                },
                cancellationToken: cancellationToken));

        return count > 0;
    }


    public async Task<DateOnly?> GetLatestResultTradeDateAsync(
        string strategyName,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT MAX(TradeDate)
            FROM StockSelectionRuns
            WHERE StrategyName = @StrategyName;
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var latestTradeDate = await connection.QuerySingleOrDefaultAsync<DateTime?>(
            new CommandDefinition(
                sql,
                new { StrategyName = strategyName },
                cancellationToken: cancellationToken));

        return latestTradeDate is null ? null : DateOnly.FromDateTime(latestTradeDate.Value);
    }

    public async Task<IReadOnlyCollection<DateOnly>> GetResultTradeDatesAsync(
        string strategyName,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TradeDate
            FROM StockSelectionRuns
            WHERE StrategyName = @StrategyName
            ORDER BY TradeDate DESC
            LIMIT @Limit;
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var rows = await connection.QueryAsync<DateTime>(
            new CommandDefinition(
                sql,
                new
                {
                    StrategyName = strategyName,
                    Limit = limit
                },
                cancellationToken: cancellationToken));

        return rows.Select(DateOnly.FromDateTime).ToArray();
    }
    public async Task<IReadOnlyCollection<StockSelectionResult>> GetStrategyResultsAsync(
        DateOnly tradeDate,
        string strategyName,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                Id,
                TradeDate,
                StrategyName,
                Rank,
                StockCode,
                StockName,
                ClosingPrice,
                PriceChange,
                PriceChangePercent,
                TradeValue,
                VolumeRatio,
                AverageClose5,
                AverageClose10,
                Score,
                Reason,
                SelectedAtUtc
            FROM StockSelectionResults
            WHERE TradeDate = @TradeDate
              AND StrategyName = @StrategyName
            ORDER BY Rank;
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var rows = await connection.QueryAsync<StockSelectionResultRow>(
            new CommandDefinition(
                sql,
                new
                {
                    TradeDate = FormatTradeDate(tradeDate),
                    StrategyName = strategyName
                },
                cancellationToken: cancellationToken));

        return rows.Select(row => row.ToEntity()).ToArray();
    }

    public async Task ReplaceStrategyResultsAsync(
        DateOnly tradeDate,
        string strategyName,
        IReadOnlyCollection<StockSelectionResult> results,
        CancellationToken cancellationToken = default)
    {
        var selectedAtUtc = DateTimeOffset.UtcNow.UtcDateTime;
        var rows = results.Select(result => new
        {
            TradeDate = FormatTradeDate(tradeDate),
            StrategyName = strategyName,
            result.Rank,
            result.StockCode,
            result.StockName,
            result.ClosingPrice,
            result.PriceChange,
            result.PriceChangePercent,
            result.TradeValue,
            result.VolumeRatio,
            result.AverageClose5,
            result.AverageClose10,
            result.Score,
            result.Reason,
            SelectedAtUtc = selectedAtUtc
        }).ToArray();

        const string deleteSql = """
            DELETE FROM StockSelectionResults
            WHERE TradeDate = @TradeDate
              AND StrategyName = @StrategyName;
            """;

        const string insertSql = """
            INSERT INTO StockSelectionResults (
                TradeDate,
                StrategyName,
                Rank,
                StockCode,
                StockName,
                ClosingPrice,
                PriceChange,
                PriceChangePercent,
                TradeValue,
                VolumeRatio,
                AverageClose5,
                AverageClose10,
                Score,
                Reason,
                SelectedAtUtc
            )
            VALUES (
                @TradeDate,
                @StrategyName,
                @Rank,
                @StockCode,
                @StockName,
                @ClosingPrice,
                @PriceChange,
                @PriceChangePercent,
                @TradeValue,
                @VolumeRatio,
                @AverageClose5,
                @AverageClose10,
                @Score,
                @Reason,
                @SelectedAtUtc
            );
            """;

        const string upsertRunSql = """
            INSERT INTO StockSelectionRuns (
                TradeDate,
                StrategyName,
                CandidateCount,
                ExecutedAtUtc
            )
            VALUES (
                @TradeDate,
                @StrategyName,
                @CandidateCount,
                @ExecutedAtUtc
            )
            ON DUPLICATE KEY UPDATE
                CandidateCount = VALUES(CandidateCount),
                ExecutedAtUtc = VALUES(ExecutedAtUtc);
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var parameters = new
        {
            TradeDate = FormatTradeDate(tradeDate),
            StrategyName = strategyName
        };

        await connection.ExecuteAsync(new CommandDefinition(deleteSql, parameters, transaction, cancellationToken: cancellationToken));

        if (rows.Length > 0)
        {
            await connection.ExecuteAsync(new CommandDefinition(insertSql, rows, transaction, cancellationToken: cancellationToken));
        }

        await connection.ExecuteAsync(
            new CommandDefinition(
                upsertRunSql,
                new
                {
                    TradeDate = FormatTradeDate(tradeDate),
                    StrategyName = strategyName,
                    CandidateCount = rows.Length,
                    ExecutedAtUtc = selectedAtUtc
                },
                transaction,
                cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);
    }

    private static string FormatTradeDate(DateOnly tradeDate)
    {
        return tradeDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private sealed class StockSelectionResultRow
    {
        public long Id { get; set; }

        public DateTime TradeDate { get; set; }

        public string StrategyName { get; set; } = string.Empty;

        public int Rank { get; set; }

        public string StockCode { get; set; } = string.Empty;

        public string StockName { get; set; } = string.Empty;

        public decimal ClosingPrice { get; set; }

        public decimal PriceChange { get; set; }

        public decimal PriceChangePercent { get; set; }

        public decimal TradeValue { get; set; }

        public decimal VolumeRatio { get; set; }

        public decimal AverageClose5 { get; set; }

        public decimal AverageClose10 { get; set; }

        public decimal Score { get; set; }

        public string Reason { get; set; } = string.Empty;

        public DateTime SelectedAtUtc { get; set; }

        public StockSelectionResult ToEntity()
        {
            return new StockSelectionResult
            {
                Id = Id,
                TradeDate = DateOnly.FromDateTime(TradeDate),
                StrategyName = StrategyName,
                Rank = Rank,
                StockCode = StockCode,
                StockName = StockName,
                ClosingPrice = ClosingPrice,
                PriceChange = PriceChange,
                PriceChangePercent = PriceChangePercent,
                TradeValue = TradeValue,
                VolumeRatio = VolumeRatio,
                AverageClose5 = AverageClose5,
                AverageClose10 = AverageClose10,
                Score = Score,
                Reason = Reason,
                SelectedAtUtc = new DateTimeOffset(DateTime.SpecifyKind(SelectedAtUtc, DateTimeKind.Utc))
            };
        }
    }
}

