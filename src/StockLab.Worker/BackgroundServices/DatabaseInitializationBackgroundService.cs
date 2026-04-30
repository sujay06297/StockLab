using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StockLab.Infrastructure.Data;

namespace StockLab.Worker.BackgroundServices;

public class DatabaseInitializationBackgroundService(
    StockDatabaseInitializer databaseInitializer,
    IHostApplicationLifetime applicationLifetime,
    ILogger<DatabaseInitializationBackgroundService> logger) : BackgroundService
{
    private const string JobName = "DatabaseInitialization";

    private readonly StockDatabaseInitializer _databaseInitializer = databaseInitializer;
    private readonly IHostApplicationLifetime _applicationLifetime = applicationLifetime;
    private readonly ILogger<DatabaseInitializationBackgroundService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Job {JobName} 開始執行：初始化資料庫。", JobName);
            await _databaseInitializer.InitializeAsync(stoppingToken);
            stopwatch.Stop();

            _logger.LogInformation(
                "Job {JobName} 執行完成：資料庫初始化完成，耗時 {ElapsedMilliseconds} 毫秒。",
                JobName,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {JobName} 執行失敗：資料庫初始化失敗。", JobName);
            throw;
        }
        finally
        {
            _applicationLifetime.StopApplication();
        }
    }
}
