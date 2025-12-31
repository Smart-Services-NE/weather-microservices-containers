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

        // Use Avro consumer if Schema Registry is configured, otherwise use JSON consumer
        var schemaRegistryUrl = configuration["Kafka:SchemaRegistryUrl"];
        if (!string.IsNullOrEmpty(schemaRegistryUrl) &&
            !schemaRegistryUrl.Contains("YOUR_SCHEMA_REGISTRY"))
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
