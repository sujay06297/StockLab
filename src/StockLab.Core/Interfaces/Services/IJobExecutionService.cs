using StockLab.Core.Models;

namespace StockLab.Core.Interfaces.Services;

public interface IJobExecutionService
{
    Task<JobRunResult<int>> RunStockSyncAsync(CancellationToken cancellationToken = default);

    Task<JobRunResult<StockSelectionQueryResult>> RunStockSelectionAsync(
        bool force = false,
        CancellationToken cancellationToken = default);
}
