using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using WeatherService.Contracts;

namespace WeatherService.Accessors;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWeatherServiceAccessors(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<WeatherDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("SubscriptionDb") ?? "Data Source=subscriptions.db"));

        services.AddHttpClient<IWeatherDataAccessor, WeatherDataAccessor>(client =>
        {
            client.BaseAddress = new Uri("https://api.open-meteo.com/");
        });

        services.AddHttpClient<IGeoCodingAccessor, GeoCodingAccessor>(client =>
        {
            client.BaseAddress = new Uri("http://api.zippopotam.us/");
        });

        services.AddScoped<IAlertPublisherAccessor, AlertPublisherAccessor>();
        services.AddScoped<ISubscriptionAccessor, SubscriptionAccessor>();

        return services;
    }
}