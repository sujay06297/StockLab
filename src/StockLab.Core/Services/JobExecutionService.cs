using System.Text;
using StockLab.Core.Interfaces.Notifications;
using StockLab.Core.Interfaces.Repositories;
using StockLab.Core.Interfaces.Services;
using StockLab.Core.Models;

namespace StockLab.Core.Services;

public class JobExecutionService(
    IStockDailyQuoteRepository stockDailyQuoteRepository,
    IStockSelectionResultRepository stockSelectionResultRepository,
    IStockSyncService stockSyncService,
    IStockSelectionService stockSelectionService,
    INotificationSender notificationSender) : IJobExecutionService
{
    private const string StockSyncJobName = "StockDayAllSync";
    private const string StockSelectionJobName = "StockMomentumSelection";

    private readonly IStockDailyQuoteRepository _stockDailyQuoteRepository = stockDailyQuoteRepository;
    private readonly IStockSelectionResultRepository _stockSelectionResultRepository = stockSelectionResultRepository;
    private readonly IStockSyncService _stockSyncService = stockSyncService;
    private readonly IStockSelectionService _stockSelectionService = stockSelectionService;
    private readonly INotificationSender _notificationSender = notificationSender;

    public async Task<JobRunResult<int>> RunStockSyncAsync(CancellationToken cancellationToken = default)
    {
        var startedAt = DateTimeOffset.Now;

        try
        {
            var recordCount = await _stockSyncService.SyncStockDayAllAsync(cancellationToken);
            await SendNotificationSafelyAsync(
                $"手動觸發 Job {StockSyncJobName} 執行完成\n開始時間：{startedAt:yyyy-MM-dd HH:mm:ss zzz}\n同步筆數：{recordCount}",
                cancellationToken);

            return new JobRunResult<int>
            {
                JobName = StockSyncJobName,
                StartedAt = startedAt,
                Source = "computed",
                Message = "同步每日股票行情完成。",
                Data = recordCount
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await SendNotificationSafelyAsync(
                $"手動觸發 Job {StockSyncJobName} 執行失敗\n開始時間：{startedAt:yyyy-MM-dd HH:mm:ss zzz}\n錯誤：{ex.Message}",
                cancellationToken);
            throw;
        }
    }

    public async Task<JobRunResult<StockSelectionQueryResult>> RunStockSelectionAsync(
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        var startedAt = DateTimeOffset.Now;
        var latestTradeDate = await _stockDailyQuoteRepository.GetLatestTradeDateAsync(cancellationToken);
        if (latestTradeDate is null)
        {
            return new JobRunResult<StockSelectionQueryResult>
            {
                JobName = StockSelectionJobName,
                StartedAt = startedAt,
                Source = "none",
                Message = "尚未有股票行情資料，請先執行同步 job。"
            };
        }

        var hasRun = await _stockSelectionResultRepository.HasStrategyRunAsync(
            latestTradeDate.Value,
            StockSelectionService.MomentumStrategyName,
            cancellationToken);
        if (hasRun && !force)
        {
            var existingResult = await BuildSelectionQueryResultAsync(latestTradeDate.Value, cancellationToken);
            return new JobRunResult<StockSelectionQueryResult>
            {
                JobName = StockSelectionJobName,
                StartedAt = startedAt,
                TradeDate = latestTradeDate.Value,
                Source = "database",
                Message = "今日已執行過選股，直接回傳資料庫結果。",
                Data = existingResult
            };
        }

        try
        {
            var candidates = await _stockSelectionService.SelectMomentumCandidatesAsync(cancellationToken);
            var storedResult = await BuildSelectionQueryResultAsync(latestTradeDate.Value, cancellationToken);
            await SendStockSelectionNotificationAsync(candidates, cancellationToken);

            return new JobRunResult<StockSelectionQueryResult>
            {
                JobName = StockSelectionJobName,
                StartedAt = startedAt,
                TradeDate = latestTradeDate.Value,
                Source = force && hasRun ? "recomputed" : "computed",
                Message = force && hasRun
                    ? "強制重算動能候選股票完成，結果已覆寫資料庫。"
                    : "挑選動能候選股票完成，結果已寫入資料庫。",
                Data = storedResult
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await SendNotificationSafelyAsync(
                $"手動觸發 Job {StockSelectionJobName} 執行失敗\n開始時間：{startedAt:yyyy-MM-dd HH:mm:ss zzz}\n錯誤：{ex.Message}",
                cancellationToken);
            throw;
        }
    }

    private async Task<StockSelectionQueryResult> BuildSelectionQueryResultAsync(
        DateOnly tradeDate,
        CancellationToken cancellationToken)
    {
        var results = await _stockSelectionResultRepository.GetStrategyResultsAsync(
            tradeDate,
            StockSelectionService.MomentumStrategyName,
            cancellationToken);

        return new StockSelectionQueryResult
        {
            StrategyName = StockSelectionService.MomentumStrategyName,
            TradeDate = tradeDate,
            CandidateCount = results.Count,
            Candidates = results
        };
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
            // 通知失敗不應影響主要 job 執行結果。
        }
    }
}
