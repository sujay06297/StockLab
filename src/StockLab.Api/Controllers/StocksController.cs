using Microsoft.AspNetCore.Mvc;
using StockLab.Core.Interfaces.Services;

namespace StockLab.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StocksController(IStockQuoteQueryService stockQuoteQueryService) : ControllerBase
{
    private readonly IStockQuoteQueryService _stockQuoteQueryService = stockQuoteQueryService;

    /// <summary>
    /// 檢查股票 API 是否正常回應。
    /// </summary>
    /// <returns>API 健康狀態。</returns>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "ok" });
    }

    /// <summary>
    /// 查詢指定股票代號的最新一筆每日行情。
    /// </summary>
    /// <param name="stockCode">股票代號，例如 2330。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>指定股票的最新每日行情；若查無資料則回傳 404。</returns>
    [HttpGet("{stockCode}/quotes/latest")]
    public async Task<IActionResult> GetLatestQuoteAsync(
        string stockCode,
        CancellationToken cancellationToken)
    {
        var latestQuote = await _stockQuoteQueryService.GetLatestQuoteAsync(stockCode, cancellationToken);
        return latestQuote is null
            ? NotFound(new { message = $"找不到股票代號 {stockCode} 的行情資料。" })
            : Ok(latestQuote);
    }
}
