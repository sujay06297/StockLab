using System.Data.Common;

namespace StockLab.Infrastructure.Data;

public interface IStockDbConnectionFactory
{
    DbConnection CreateConnection();
}
