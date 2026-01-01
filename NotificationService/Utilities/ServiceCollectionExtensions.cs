using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Contracts;

namespace NotificationService.Utilities;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationServiceUtilities(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<ITelemetryUtility, TelemetryUtility>();
        services.AddScoped<IRetryPolicyUtility, RetryPolicyUtility>();

        // Use Avro consumer if explicitly enabled, otherwise use JSON consumer
        var useAvroConsumer = configuration.GetValue<bool>("Kafka:UseAvroConsumer", defaultValue: false);

        if (useAvroConsumer)
        {
            services.AddSingleton<IKafkaConsumerUtility, AvroKafkaConsumerUtility>();
        }
        else
        {
            services.AddSingleton<IKafkaConsumerUtility, KafkaConsumerUtility>();
        }

        return services;
    }
}
