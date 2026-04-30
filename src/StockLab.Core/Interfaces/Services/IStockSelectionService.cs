using StockLab.Core.Models;

namespace StockLab.Core.Interfaces.Services;

public interface IStockSelectionService
{
    Task<IReadOnlyCollection<StockSelectionCandidate>> SelectMomentumCandidatesAsync(
        CancellationToken cancellationToken = default);
}
