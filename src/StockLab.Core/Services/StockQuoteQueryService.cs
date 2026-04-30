using StockLab.Core.Entities;
using StockLab.Core.Interfaces.Repositories;
using StockLab.Core.Interfaces.Services;

namespace StockLab.Core.Services;

public class StockQuoteQueryService(IStockDailyQuoteRepository stockDailyQuoteRepository) : IStockQuoteQueryService
{
    private readonly IStockDailyQuoteRepository _stockDailyQuoteRepository = stockDailyQuoteRepository;

    public async Task<StockDailyQuote?> GetLatestQuoteAsync(
        string stockCode,
        CancellationToken cancellationToken = default)
    {
        var quotes = await _stockDailyQuoteRepository.GetByStockCodeAsync(
            stockCode,
            cancellationToken: cancellationToken);

        return quotes.FirstOrDefault();
    }
}
