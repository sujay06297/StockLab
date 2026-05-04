using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StockLab.Core.Interfaces.Clients;
using StockLab.Core.Interfaces.Notifications;
using StockLab.Core.Interfaces.Repositories;
using StockLab.Core.Interfaces.Services;
using StockLab.Core.Services;
using StockLab.Infrastructure.Data;
using StockLab.Infrastructure.Http;
using StockLab.Infrastructure.Notifications;
using StockLab.Infrastructure.Options;
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
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            })
            .ConfigureServices((context, services) =>
            {
                var connectionString = context.Configuration.GetConnectionString("StockDb")
                    ?? throw new InvalidOperationException("缺少資料庫連線字串設定：ConnectionStrings:StockDb。");

                services.Configure<TelegramOptions>(context.Configuration.GetSection("Telegram"));

                services.AddHttpClient("twse")
                    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                    {
                        UseProxy = false
                    });

                services.AddHttpClient("telegram", client =>
                {
                    client.BaseAddress = new Uri("https://api.telegram.org/");
                    client.Timeout = TimeSpan.FromSeconds(30);
                });

                services.AddSingleton<IStockDbConnectionFactory>(_ => new MySqlStockDbConnectionFactory(connectionString));
                services.AddSingleton<StockDatabaseInitializer>();
                services.AddSingleton<INotificationSender, TelegramNotificationSender>();
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
