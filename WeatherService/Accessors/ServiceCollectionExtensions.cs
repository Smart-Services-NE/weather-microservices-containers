using Microsoft.Extensions.DependencyInjection;
using WeatherService.Contracts;

namespace WeatherService.Accessors;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWeatherServiceAccessors(this IServiceCollection services)
    {
        services.AddHttpClient<IWeatherDataAccessor, WeatherDataAccessor>(client =>
        {
            client.BaseAddress = new Uri("https://api.open-meteo.com/");
        });

        services.AddHttpClient<IGeoCodingAccessor, GeoCodingAccessor>(client =>
        {
            client.BaseAddress = new Uri("http://api.zippopotam.us/");
        });

        return services;
    }
}