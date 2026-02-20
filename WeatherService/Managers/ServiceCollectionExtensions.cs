using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WeatherService.Contracts;
using WeatherService.Accessors;
using WeatherService.Engines;
using WeatherService.Utilities;

namespace WeatherService.Managers;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWeatherServiceManagers(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IWeatherManager, WeatherManager>();
        services.AddScoped<ISubscriptionManager, SubscriptionManager>();
        services.AddWeatherServiceAccessors(configuration);
        services.AddWeatherServiceEngines();
        services.AddWeatherServiceUtilities();
        return services;
    }
}