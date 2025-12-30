using Microsoft.Extensions.DependencyInjection;
using WeatherService.Contracts;

namespace WeatherService.Engines;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWeatherServiceEngines(this IServiceCollection services)
    {
        services.AddScoped<IWeatherCodeEngine, WeatherCodeEngine>();
        return services;
    }
}