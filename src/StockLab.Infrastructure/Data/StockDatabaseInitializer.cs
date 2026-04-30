using Dapper;

namespace StockLab.Infrastructure.Data;

public class StockDatabaseInitializer(IStockDbConnectionFactory connectionFactory)
{
    private readonly IStockDbConnectionFactory _connectionFactory = connectionFactory;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_connectionFactory is MySqlStockDbConnectionFactory mySqlConnectionFactory)
        {
            await mySqlConnectionFactory.CreateDatabaseIfNotExistsAsync(cancellationToken);
        }

        const string sql = """
            CREATE TABLE IF NOT EXISTS StockDailyQuotes (
                Id BIGINT NOT NULL AUTO_INCREMENT,
                TradeDate DATE NOT NULL,
                StockCode VARCHAR(16) NOT NULL,
                StockName VARCHAR(64) NOT NULL,
                TradeVolume BIGINT NULL,
                TradeValue DECIMAL(20, 2) NULL,
                OpeningPrice DECIMAL(18, 4) NULL,
                HighestPrice DECIMAL(18, 4) NULL,
                LowestPrice DECIMAL(18, 4) NULL,
                ClosingPrice DECIMAL(18, 4) NULL,
                PriceChange DECIMAL(18, 4) NULL,
                PriceChangeText VARCHAR(32) NOT NULL,
                TransactionCount BIGINT NULL,
                SyncedAtUtc DATETIME(6) NOT NULL,
                PRIMARY KEY (Id),
                UNIQUE KEY UX_StockDailyQuotes_TradeDate_StockCode (TradeDate, StockCode),
                KEY IX_StockDailyQuotes_StockCode_TradeDate (StockCode, TradeDate)
            );

            CREATE TABLE IF NOT EXISTS StockSelectionRuns (
                Id BIGINT NOT NULL AUTO_INCREMENT,
                TradeDate DATE NOT NULL,
                StrategyName VARCHAR(64) NOT NULL,
                CandidateCount INT NOT NULL,
                ExecutedAtUtc DATETIME(6) NOT NULL,
                PRIMARY KEY (Id),
                UNIQUE KEY UX_StockSelectionRuns_TradeDate_StrategyName (TradeDate, StrategyName)
            );

            CREATE TABLE IF NOT EXISTS StockSelectionResults (
                Id BIGINT NOT NULL AUTO_INCREMENT,
                TradeDate DATE NOT NULL,
                StrategyName VARCHAR(64) NOT NULL,
                Rank INT NOT NULL,
                StockCode VARCHAR(16) NOT NULL,
                StockName VARCHAR(64) NOT NULL,
                ClosingPrice DECIMAL(18, 4) NOT NULL,
                PriceChange DECIMAL(18, 4) NOT NULL,
                PriceChangePercent DECIMAL(18, 4) NOT NULL,
                TradeValue DECIMAL(20, 2) NOT NULL,
                VolumeRatio DECIMAL(18, 4) NOT NULL,
                AverageClose5 DECIMAL(18, 4) NOT NULL,
                AverageClose10 DECIMAL(18, 4) NOT NULL,
                Score DECIMAL(18, 4) NOT NULL,
                Reason VARCHAR(512) NOT NULL,
                SelectedAtUtc DATETIME(6) NOT NULL,
                PRIMARY KEY (Id),
                UNIQUE KEY UX_StockSelectionResults_Strategy_TradeDate_StockCode (StrategyName, TradeDate, StockCode),
                KEY IX_StockSelectionResults_Strategy_TradeDate_Rank (StrategyName, TradeDate, Rank)
            );
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }
}
