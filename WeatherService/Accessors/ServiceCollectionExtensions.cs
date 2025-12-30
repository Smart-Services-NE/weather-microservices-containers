using Microsoft.Extensions.DependencyInjection;
using WeatherService.Contracts;

namespace WeatherService.Accessors;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWeatherServiceAccessors(this IServiceCollection services)
    {
        services.AddScoped<IWeatherDataAccessor, WeatherDataAccessor>();
        services.AddScoped<IGeoCodingAccessor, GeoCodingAccessor>();
        return services;
    }
}