using Microsoft.AspNetCore.Mvc;
using StockLab.Core.Interfaces.Services;

namespace StockLab.Api.Controllers;

[ApiController]
[Route("api/stock-selections")]
public class StockSelectionsController(IStockSelectionQueryService stockSelectionQueryService) : ControllerBase
{
    private readonly IStockSelectionQueryService _stockSelectionQueryService = stockSelectionQueryService;

    /// <summary>
    /// 查詢最新交易日的動能選股結果。
    /// </summary>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>最新選股交易日、候選檔數與候選股票清單；若尚無結果則回傳 404。</returns>
    [HttpGet("latest")]
    public async Task<IActionResult> GetLatestAsync(CancellationToken cancellationToken)
    {
        var result = await _stockSelectionQueryService.GetLatestAsync(cancellationToken);
        return result is null
            ? NotFound(new { message = "尚未有選股結果。" })
            : Ok(result);
    }

    /// <summary>
    /// 查詢指定交易日的動能選股結果。
    /// </summary>
    /// <param name="tradeDate">交易日期，格式為 yyyy-MM-dd。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>指定交易日的候選檔數與候選股票清單；若該日尚未執行則回傳 404。</returns>
    [HttpGet("{tradeDate}")]
    public async Task<IActionResult> GetByTradeDateAsync(
        DateOnly tradeDate,
        CancellationToken cancellationToken)
    {
        var result = await _stockSelectionQueryService.GetByTradeDateAsync(tradeDate, cancellationToken);
        return result is null
            ? NotFound(new { message = $"交易日 {tradeDate:yyyy-MM-dd} 尚未有選股結果。" })
            : Ok(result);
    }

    /// <summary>
    /// 查詢最近有執行動能選股的交易日期清單。
    /// </summary>
    /// <param name="limit">最多回傳幾個交易日；允許範圍為 1 到 100。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>最近有選股執行紀錄的交易日期清單。</returns>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistoryAsync(
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _stockSelectionQueryService.GetHistoryAsync(limit, cancellationToken);
        return Ok(result);
    }
}

