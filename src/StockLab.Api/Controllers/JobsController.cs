using System.Text;
using Microsoft.AspNetCore.Mvc;
using StockLab.Core.Entities;
using StockLab.Core.Interfaces.Notifications;
using StockLab.Core.Interfaces.Repositories;
using StockLab.Core.Interfaces.Services;
using StockLab.Core.Models;
using StockLab.Core.Services;

namespace StockLab.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController(
    IStockDailyQuoteRepository stockDailyQuoteRepository,
    IStockSelectionResultRepository stockSelectionResultRepository,
    IStockSyncService stockSyncService,
    IStockSelectionService stockSelectionService,
    INotificationSender notificationSender,
    ILogger<JobsController> logger) : ControllerBase
{
    private const string StockSyncJobName = "StockDayAllSync";
    private const string StockSelectionJobName = "StockMomentumSelection";

    private readonly IStockDailyQuoteRepository _stockDailyQuoteRepository = stockDailyQuoteRepository;
    private readonly IStockSelectionResultRepository _stockSelectionResultRepository = stockSelectionResultRepository;
    private readonly IStockSyncService _stockSyncService = stockSyncService;
    private readonly IStockSelectionService _stockSelectionService = stockSelectionService;
    private readonly INotificationSender _notificationSender = notificationSender;
    private readonly ILogger<JobsController> _logger = logger;

    [HttpPost("stock-sync/run")]
    public async Task<IActionResult> RunStockSyncAsync(CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.Now;

        try
        {
            _logger.LogInformation("手動觸發 Job {JobName}：同步每日股票行情。", StockSyncJobName);
            var recordCount = await _stockSyncService.SyncStockDayAllAsync(cancellationToken);

            await SendNotificationSafelyAsync(
                $"手動觸發 Job {StockSyncJobName} 執行完成\n開始時間：{startedAt:yyyy-MM-dd HH:mm:ss zzz}\n同步筆數：{recordCount}",
                cancellationToken);

            return Ok(new
            {
                jobName = StockSyncJobName,
                startedAt,
                recordCount,
                message = "同步每日股票行情完成。"
            });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "手動觸發 Job {JobName} 失敗：同步每日股票行情失敗。", StockSyncJobName);
            await SendNotificationSafelyAsync(
                $"手動觸發 Job {StockSyncJobName} 執行失敗\n開始時間：{startedAt:yyyy-MM-dd HH:mm:ss zzz}\n錯誤：{ex.Message}",
                cancellationToken);
            throw;
        }
    }

    [HttpPost("stock-selection/run")]
    public async Task<IActionResult> RunStockSelectionAsync([FromQuery] bool force, CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.Now;
        var latestTradeDate = await _stockDailyQuoteRepository.GetLatestTradeDateAsync(cancellationToken);
        if (latestTradeDate is null)
        {
            return BadRequest(new { message = "尚未有股票行情資料，請先執行同步 job。" });
        }

        var hasRun = await _stockSelectionResultRepository.HasStrategyRunAsync(
            latestTradeDate.Value,
            StockSelectionService.MomentumStrategyName,
            cancellationToken);
        if (hasRun && !force)
        {
            var existingResults = await _stockSelectionResultRepository.GetStrategyResultsAsync(
                latestTradeDate.Value,
                StockSelectionService.MomentumStrategyName,
                cancellationToken);

            return Ok(new
            {
                jobName = StockSelectionJobName,
                tradeDate = latestTradeDate.Value,
                source = "database",
                candidateCount = existingResults.Count,
                candidates = existingResults,
                message = "今日已執行過選股，直接回傳資料庫結果。"
            });
        }

        try
        {
            _logger.LogInformation("手動觸發 Job {JobName}：挑選動能候選股票。", StockSelectionJobName);
            var candidates = await _stockSelectionService.SelectMomentumCandidatesAsync(cancellationToken);
            var storedResults = await _stockSelectionResultRepository.GetStrategyResultsAsync(
                latestTradeDate.Value,
                StockSelectionService.MomentumStrategyName,
                cancellationToken);
            await SendStockSelectionNotificationAsync(candidates, cancellationToken);

            return Ok(new
            {
                jobName = StockSelectionJobName,
                startedAt,
                tradeDate = latestTradeDate.Value,
                source = force && hasRun ? "recomputed" : "computed",
                candidateCount = storedResults.Count,
                candidates = storedResults,
                message = force && hasRun ? "強制重算動能候選股票完成，結果已覆寫資料庫。" : "挑選動能候選股票完成，結果已寫入資料庫。"
            });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "手動觸發 Job {JobName} 失敗：挑選動能候選股票失敗。", StockSelectionJobName);
            await SendNotificationSafelyAsync(
                $"手動觸發 Job {StockSelectionJobName} 執行失敗\n開始時間：{startedAt:yyyy-MM-dd HH:mm:ss zzz}\n錯誤：{ex.Message}",
                cancellationToken);
            throw;
        }
    }

    private async Task SendStockSelectionNotificationAsync(
        IReadOnlyCollection<StockSelectionCandidate> candidates,
        CancellationToken cancellationToken)
    {
        if (candidates.Count == 0)
        {
            await SendNotificationSafelyAsync(
                $"手動觸發 Job {StockSelectionJobName} 執行完成\n今日沒有符合策略條件的候選股票。",
                cancellationToken);
            return;
        }

        var tradeDate = candidates.First().TradeDate;
        var message = new StringBuilder()
            .AppendLine($"手動觸發 Job {StockSelectionJobName} 執行完成")
            .AppendLine($"交易日：{tradeDate:yyyy-MM-dd}")
            .AppendLine($"候選股票：{candidates.Count} 檔")
            .AppendLine();

        var rank = 1;
        foreach (var candidate in candidates)
        {
            message
                .AppendLine($"{rank}. {candidate.StockCode} {candidate.StockName}")
                .AppendLine($"   收盤：{candidate.ClosingPrice}，漲跌：{candidate.PriceChange} ({candidate.PriceChangePercent}%)")
                .AppendLine($"   量比：{candidate.VolumeRatio}，5日均：{candidate.AverageClose5}，10日均：{candidate.AverageClose10}，分數：{candidate.Score}");
            rank++;
        }

        await SendNotificationSafelyAsync(message.ToString(), cancellationToken);
    }

    private async Task SendNotificationSafelyAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            await _notificationSender.SendAsync(message, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "手動觸發 Job 的通知送出失敗。");
        }
    }
}

