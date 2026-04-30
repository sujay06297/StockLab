using System.Data.Common;
using MySqlConnector;

namespace StockLab.Infrastructure.Data;

public class MySqlStockDbConnectionFactory(string connectionString) : IStockDbConnectionFactory
{
    private readonly string _connectionString = connectionString;
    private readonly MySqlConnectionStringBuilder _connectionStringBuilder = new(connectionString);

    public DbConnection CreateConnection()
    {
        return new MySqlConnection(_connectionString);
    }

    public async Task CreateDatabaseIfNotExistsAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionStringBuilder.Database))
        {
            throw new InvalidOperationException("MySQL connection string must include a database name.");
        }

        var serverConnectionStringBuilder = new MySqlConnectionStringBuilder(_connectionString)
        {
            Database = string.Empty
        };

        await using var connection = new MySqlConnection(serverConnectionStringBuilder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        var databaseName = EscapeIdentifier(_connectionStringBuilder.Database);
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE IF NOT EXISTS `{databaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string EscapeIdentifier(string identifier)
    {
        return identifier.Replace("`", "``", StringComparison.Ordinal);
    }
}
