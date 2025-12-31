using Microsoft.Extensions.DependencyInjection;
using NotificationService.Contracts;

namespace NotificationService.Utilities;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationServiceUtilities(this IServiceCollection services)
    {
        services.AddScoped<ITelemetryUtility, TelemetryUtility>();
        services.AddScoped<IRetryPolicyUtility, RetryPolicyUtility>();
        services.AddSingleton<IKafkaConsumerUtility, KafkaConsumerUtility>();

        return services;
    }
}
