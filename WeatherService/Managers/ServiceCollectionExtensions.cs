using Microsoft.Extensions.DependencyInjection;
using WeatherService.Contracts;
using WeatherService.Accessors;
using WeatherService.Engines;
using WeatherService.Utilities;

namespace WeatherService.Managers;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWeatherServiceManagers(this IServiceCollection services)
    {
        services.AddScoped<IWeatherManager, WeatherManager>();
        services.AddWeatherServiceAccessors();
        services.AddWeatherServiceEngines();
        services.AddWeatherServiceUtilities();
        return services;
    }
}