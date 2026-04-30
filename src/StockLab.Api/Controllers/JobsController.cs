using Microsoft.AspNetCore.Mvc;
using StockLab.Core.Interfaces.Services;

namespace StockLab.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController(IJobExecutionService jobExecutionService) : ControllerBase
{
    private readonly IJobExecutionService _jobExecutionService = jobExecutionService;

    /// <summary>
    /// 建立一筆每日股票行情同步 job run，並立即執行同步。
    /// </summary>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>同步筆數與 job run 執行狀態；執行完成或失敗都會嘗試發送通知。</returns>
    [HttpPost("stock-sync/runs")]
    [HttpPost("stock-sync/run")]
    public async Task<IActionResult> RunStockSyncAsync(CancellationToken cancellationToken)
    {
        var result = await _jobExecutionService.RunStockSyncAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// 建立一筆動能選股 job run，並立即執行或回傳同交易日既有結果。
    /// </summary>
    /// <param name="force">是否強制重算並覆寫同交易日的既有選股結果。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>選股候選清單、資料來源與 job run 執行狀態。</returns>
    [HttpPost("stock-selection/runs")]
    [HttpPost("stock-selection/run")]
    public async Task<IActionResult> RunStockSelectionAsync([FromQuery] bool force, CancellationToken cancellationToken)
    {
        var result = await _jobExecutionService.RunStockSelectionAsync(force, cancellationToken);
        return result.Data is null
            ? BadRequest(result)
            : Ok(result);
    }
}
