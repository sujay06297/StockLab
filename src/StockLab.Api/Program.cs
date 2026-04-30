using StockLab.Api.Middleware;
using StockLab.Core.Interfaces.Repositories;
using StockLab.Infrastructure.Data;
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
        builder.Services.AddSingleton<IStockDbConnectionFactory>(_ => new MySqlStockDbConnectionFactory(connectionString));
        builder.Services.AddSingleton<StockDatabaseInitializer>();
        builder.Services.AddScoped<IStockDailyQuoteRepository, StockDailyQuoteRepository>();

        var app = builder.Build();

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var databaseInitializer = scope.ServiceProvider.GetRequiredService<StockDatabaseInitializer>();
            await databaseInitializer.InitializeAsync();
        }

        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.MapControllers();
        app.MapGet("/", () => Results.Ok(new { message = "StockLab.Api is running" }));

        await app.RunAsync();
    }
}
