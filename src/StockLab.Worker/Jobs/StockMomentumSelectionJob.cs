using System.Text;
using Microsoft.Extensions.Logging;
using Quartz;
using StockLab.Core.Interfaces.Notifications;
using StockLab.Core.Interfaces.Repositories;
using StockLab.Core.Interfaces.Services;
using StockLab.Core.Models;
using StockLab.Core.Services;

namespace StockLab.Worker.Jobs;

[DisallowConcurrentExecution]
public class StockMomentumSelectionJob(
    IStockDailyQuoteRepository stockDailyQuoteRepository,
    IStockSelectionResultRepository stockSelectionResultRepository,
    IStockSelectionService stockSelectionService,
    INotificationSender notificationSender,
    ILogger<StockMomentumSelectionJob> logger) : IJob
{
    public const string JobName = "StockMomentumSelection";
    public const string DefaultCronExpression = "0 35 17 * * ?";
    public const string DefaultTimeZoneId = "Taipei Standard Time";

    private readonly IStockDailyQuoteRepository _stockDailyQuoteRepository = stockDailyQuoteRepository;
    private readonly IStockSelectionResultRepository _stockSelectionResultRepository = stockSelectionResultRepository;
    private readonly IStockSelectionService _stockSelectionService = stockSelectionService;
    private readonly INotificationSender _notificationSender = notificationSender;
    private readonly ILogger<StockMomentumSelectionJob> _logger = logger;

    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;

        try
        {
            var latestTradeDate = await _stockDailyQuoteRepository.GetLatestTradeDateAsync(cancellationToken);
            if (latestTradeDate is null)
            {
                _logger.LogWarning("Job {JobName} 略過執行：尚未有股票行情資料。", JobName);
                return;
            }

            _logger.LogInformation(
                "Job {JobName} 開始執行：重新挑選交易日 {TradeDate} 的動能候選股票。",
                JobName,
                latestTradeDate.Value);
            var candidates = await _stockSelectionService.SelectMomentumCandidatesAsync(cancellationToken);

            _logger.LogInformation(
                "Job {JobName} 執行完成：挑出 {CandidateCount} 檔候選股票。",
                JobName,
                candidates.Count);

            await SendSelectionResultAsync(candidates, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Job {JobName} 執行失敗：挑選動能候選股票失敗。", JobName);
            await SendNotificationSafelyAsync(
                $"Job {JobName} 執行失敗\n錯誤：{ex.Message}",
                cancellationToken);

            throw new JobExecutionException(ex, refireImmediately: false);
        }
    }

    private async Task SendSelectionResultAsync(
        IReadOnlyCollection<StockSelectionCandidate> candidates,
        CancellationToken cancellationToken)
    {
        if (candidates.Count == 0)
        {
            await SendNotificationSafelyAsync(
                $"Job {JobName} 執行完成\n今日沒有符合策略條件的候選股票。",
                cancellationToken);
            return;
        }

        var tradeDate = candidates.First().TradeDate;
        var message = new StringBuilder()
            .AppendLine($"Job {JobName} 執行完成")
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
            _logger.LogError(ex, "Job {JobName} 通知送出失敗。", JobName);
        }
    }
}
