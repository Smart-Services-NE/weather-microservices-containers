using Microsoft.Extensions.DependencyInjection;
using NotificationService.Contracts;

namespace NotificationService.Engines;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationServiceEngines(this IServiceCollection services)
    {
        services.AddScoped<INotificationEngine, NotificationEngine>();

        return services;
    }
}
