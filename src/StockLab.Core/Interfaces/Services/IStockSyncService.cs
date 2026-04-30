namespace StockLab.Core.Interfaces.Services;

public interface IStockSyncService
{
    Task<int> SyncStockDayAllAsync(CancellationToken cancellationToken = default);
}
