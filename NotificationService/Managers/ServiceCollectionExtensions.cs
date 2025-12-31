using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Accessors;
using NotificationService.Contracts;
using NotificationService.Engines;
using NotificationService.Utilities;

namespace NotificationService.Managers;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationServiceManagers(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<INotificationManager, NotificationManager>();

        services.AddNotificationServiceAccessors(configuration);
        services.AddNotificationServiceEngines();
        services.AddNotificationServiceUtilities(configuration);

        return services;
    }
}
