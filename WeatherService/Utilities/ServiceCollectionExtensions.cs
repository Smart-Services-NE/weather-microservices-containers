using Microsoft.Extensions.DependencyInjection;
using WeatherService.Contracts;

namespace WeatherService.Utilities;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWeatherServiceUtilities(this IServiceCollection services)
    {
        services.AddScoped<ICacheUtility, CacheUtility>();
        services.AddScoped<ITelemetryUtility, TelemetryUtility>();
        services.AddSingleton<IRetryPolicyUtility, RetryPolicyUtility>();
        return services;
    }
}