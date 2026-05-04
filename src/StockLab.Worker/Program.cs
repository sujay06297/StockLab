using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;
using StockLab.Core.Interfaces.Clients;
using StockLab.Core.Interfaces.Repositories;
using StockLab.Core.Interfaces.Services;
using StockLab.Core.Services;
using StockLab.Infrastructure.Data;
using StockLab.Infrastructure.Http;
using StockLab.Infrastructure.Notifications;
using StockLab.Infrastructure.Repositories;
using StockLab.Worker.BackgroundServices;
using StockLab.Worker.Jobs;

namespace StockLab.Worker;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var initializeDatabaseOnly = args.Contains("--init-db", StringComparer.OrdinalIgnoreCase);

        var host = Host.CreateDefaultBuilder(args)
            .UseWindowsService(options =>
            {
                options.ServiceName = "StockLab Worker";
            })
            .ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                if (OperatingSystem.IsWindows() && WindowsServiceHelpers.IsWindowsService())
                {
#pragma warning disable CA1416
                    logging.AddEventLog(settings =>
                    {
                        settings.SourceName = "StockLab Worker";
                    });
#pragma warning restore CA1416
                }
            })
            .ConfigureServices((context, services) =>
            {
                var connectionString = context.Configuration.GetConnectionString("StockDb")
                    ?? throw new InvalidOperationException("缺少資料庫連線字串設定：ConnectionStrings:StockDb。");

                services.AddStockLabNotifications(context.Configuration);

                services.AddHttpClient("twse")
                    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                    {
                        UseProxy = false
                    });

                services.AddSingleton<IStockDbConnectionFactory>(_ => new MySqlStockDbConnectionFactory(connectionString));
                services.AddSingleton<StockDatabaseInitializer>();
                services.AddScoped<IStockDailyQuoteRepository, StockDailyQuoteRepository>();
                services.AddScoped<IStockSelectionResultRepository, StockSelectionResultRepository>();
                services.AddScoped<ITwseClient, TwseClient>();
                services.AddScoped<IStockSyncService, StockSyncService>();
                services.AddScoped<IStockSelectionService, StockSelectionService>();
                services.AddScoped<IJobExecutionService, JobExecutionService>();

                if (initializeDatabaseOnly)
                {
                    services.AddHostedService<DatabaseInitializationBackgroundService>();
                }
                else
                {
                    JobScheduleRegistry.AddScheduledJobs(context.Configuration, services);
                    services.AddHostedService<StartupJobRunBackgroundService>();
                }
            })
            .Build();

        await host.RunAsync();
    }
}
