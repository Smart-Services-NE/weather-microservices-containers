using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Contracts;

namespace NotificationService.Accessors;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationServiceAccessors(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<NotificationDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("NotificationDb") ?? "Data Source=notifications.db"));

        services.AddScoped<IEmailAccessor, EmailAccessor>();
        services.AddScoped<INotificationStorageAccessor, NotificationStorageAccessor>();

        return services;
    }
}
