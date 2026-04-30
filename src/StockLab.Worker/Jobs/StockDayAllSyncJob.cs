using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Quartz;
using StockLab.Core.Interfaces.Notifications;
using StockLab.Core.Interfaces.Services;

namespace StockLab.Worker.Jobs;

[DisallowConcurrentExecution]
public class StockDayAllSyncJob(
    IStockSyncService stockSyncService,
    INotificationSender notificationSender,
    ILogger<StockDayAllSyncJob> logger) : IJob
{
    public const string JobName = "StockDayAllSync";
    public const string DefaultCronExpression = "0 0 18 * * ?";
    public const string DefaultTimeZoneId = "Taipei Standard Time";

    private readonly IStockSyncService _stockSyncService = stockSyncService;
    private readonly INotificationSender _notificationSender = notificationSender;
    private readonly ILogger<StockDayAllSyncJob> _logger = logger;

    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var stopwatch = Stopwatch.StartNew();
        var startedAt = DateTimeOffset.Now;

        try
        {
            _logger.LogInformation("Job {JobName} 開始執行：同步每日股票行情。", JobName);
            var recordCount = await _stockSyncService.SyncStockDayAllAsync(cancellationToken);
            stopwatch.Stop();

            _logger.LogInformation(
                "Job {JobName} 執行完成：同步 {RecordCount} 筆資料，耗時 {ElapsedMilliseconds} 毫秒。",
                JobName,
                recordCount,
                stopwatch.ElapsedMilliseconds);

            await SendNotificationSafelyAsync(
                $"Job {JobName} 執行完成\n開始時間：{startedAt:yyyy-MM-dd HH:mm:ss zzz}\n同步筆數：{recordCount}\n耗時：{stopwatch.ElapsedMilliseconds} 毫秒",
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Job {JobName} 執行失敗：同步每日股票行情失敗。", JobName);

            await SendNotificationSafelyAsync(
                $"Job {JobName} 執行失敗\n開始時間：{startedAt:yyyy-MM-dd HH:mm:ss zzz}\n耗時：{stopwatch.ElapsedMilliseconds} 毫秒\n錯誤：{ex.Message}",
                cancellationToken);

            throw new JobExecutionException(ex, refireImmediately: false);
        }
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
