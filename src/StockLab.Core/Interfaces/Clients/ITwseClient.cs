using StockLab.Core.DTOs;

namespace StockLab.Core.Interfaces.Clients;

public interface ITwseClient
{
    Task<IReadOnlyCollection<TwseStockDayAllDto>> GetStockDayAllAsync(CancellationToken cancellationToken = default);
}
