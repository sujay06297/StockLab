using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StockLab.Core.Interfaces.Services;
using StockLab.Infrastructure.Data;

namespace StockLab.Worker.BackgroundServices;

public class StartupJobRunBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<StartupJobRunBackgroundService> logger) : BackgroundService
{
    private const string JobName = "StartupJobRun";

    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<StartupJobRunBackgroundService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var databaseInitializer = scope.ServiceProvider.GetRequiredService<StockDatabaseInitializer>();
            var jobExecutionService = scope.ServiceProvider.GetRequiredService<IJobExecutionService>();

            _logger.LogInformation("Job {JobName} 開始執行：啟動時初始化資料庫。", JobName);
            await databaseInitializer.InitializeAsync(stoppingToken);

            _logger.LogInformation("Job {JobName} 開始執行：啟動時同步每日股票行情。", JobName);
            var syncResult = await jobExecutionService.RunStockSyncAsync(stoppingToken);

            _logger.LogInformation(
                "Job {JobName} 行情同步完成：同步 {RecordCount} 筆資料。",
                JobName,
                syncResult.Data);

            _logger.LogInformation("Job {JobName} 開始執行：啟動時強制重算動能選股。", JobName);
            var selectionResult = await jobExecutionService.RunStockSelectionAsync(force: true, stoppingToken);

            stopwatch.Stop();
            _logger.LogInformation(
                "Job {JobName} 執行完成：選出 {CandidateCount} 檔候選股票，耗時 {ElapsedMilliseconds} 毫秒。",
                JobName,
                selectionResult.Data?.CandidateCount ?? 0,
                stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Job {JobName} 已取消。", JobName);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Job {JobName} 執行失敗：啟動時同步行情或選股失敗，排程服務會繼續待命。",
                JobName);
        }
    }
}
