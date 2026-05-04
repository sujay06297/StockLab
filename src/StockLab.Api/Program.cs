using StockLab.Api.Middleware;
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

namespace StockLab.Api;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var connectionString = builder.Configuration.GetConnectionString("StockDb")
            ?? throw new InvalidOperationException("缺少資料庫連線字串設定：ConnectionStrings:StockDb。");

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.Configure<TelegramOptions>(builder.Configuration.GetSection("Telegram"));

        builder.Services.AddHttpClient("twse")
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                UseProxy = false
            });

        builder.Services.AddHttpClient("telegram", client =>
        {
            client.BaseAddress = new Uri("https://api.telegram.org/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        builder.Services.AddSingleton<IStockDbConnectionFactory>(_ => new MySqlStockDbConnectionFactory(connectionString));
        builder.Services.AddSingleton<StockDatabaseInitializer>();
        builder.Services.AddSingleton<INotificationSender, TelegramNotificationSender>();
        builder.Services.AddScoped<IStockDailyQuoteRepository, StockDailyQuoteRepository>();
        builder.Services.AddScoped<IStockSelectionResultRepository, StockSelectionResultRepository>();
        builder.Services.AddScoped<ITwseClient, TwseClient>();
        builder.Services.AddScoped<IStockSyncService, StockSyncService>();
        builder.Services.AddScoped<IStockSelectionService, StockSelectionService>();
        builder.Services.AddScoped<IStockQuoteQueryService, StockQuoteQueryService>();
        builder.Services.AddScoped<IStockSelectionQueryService, StockSelectionQueryService>();
        builder.Services.AddScoped<IJobExecutionService, JobExecutionService>();

        var app = builder.Build();

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var databaseInitializer = scope.ServiceProvider.GetRequiredService<StockDatabaseInitializer>();
            await databaseInitializer.InitializeAsync();
        }

        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.MapControllers();
        app.MapGet("/", () => Results.Ok(new { message = "StockLab.Api is running" }));

        await app.RunAsync();
    }
}

