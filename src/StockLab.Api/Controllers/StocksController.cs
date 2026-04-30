using Microsoft.AspNetCore.Mvc;
using StockLab.Core.Interfaces.Repositories;

namespace StockLab.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StocksController(IStockDailyQuoteRepository stockDailyQuoteRepository) : ControllerBase
{
    private readonly IStockDailyQuoteRepository _stockDailyQuoteRepository = stockDailyQuoteRepository;

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "ok" });
    }

    [HttpGet("{stockCode}/quotes/latest")]
    public async Task<IActionResult> GetLatestQuoteAsync(
        string stockCode,
        CancellationToken cancellationToken)
    {
        var quotes = await _stockDailyQuoteRepository.GetByStockCodeAsync(
            stockCode,
            cancellationToken: cancellationToken);
        var latestQuote = quotes.FirstOrDefault();

        if (latestQuote is null)
        {
            return NotFound(new { message = $"找不到股票代號 {stockCode} 的行情資料。" });
        }

        return Ok(latestQuote);
    }
}
