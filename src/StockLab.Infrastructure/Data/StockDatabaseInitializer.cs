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
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }
}
