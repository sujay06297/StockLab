using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StockLab.Core.Interfaces.Notifications;
using StockLab.Infrastructure.Options;

namespace StockLab.Infrastructure.Notifications;

public static class NotificationServiceCollectionExtensions
{
    public static IServiceCollection AddStockLabNotifications(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<TelegramOptions>(configuration.GetSection("Telegram"));
        services.Configure<DiscordOptions>(configuration.GetSection("Discord"));

        services.AddHttpClient("telegram", client =>
        {
            client.BaseAddress = new Uri("https://api.telegram.org/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHttpClient("discord", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            UseProxy = false
        });

        services.AddSingleton<INotificationChannelSender, DiscordNotificationSender>();
        services.AddSingleton<INotificationSender, CompositeNotificationSender>();

        return services;
    }
}
