using WeatherService.Models;

namespace WeatherService.Repositories;

using Microsoft.Extensions.Caching.Hybrid;

public class WeatherRepository : IWeatherRepository
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly HybridCache _cache;

    public WeatherRepository(IHttpClientFactory httpClientFactory, HybridCache cache)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
    }

    public async Task<WeatherForecast?> GetForecastAsync(string zipCode)
    {
        return await _cache.GetOrCreateAsync(
            $"weather:{zipCode}", 
            async cancel => await FetchForecastAsync(zipCode),
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(5) }
        );
    }

    private async Task<WeatherForecast?> FetchForecastAsync(string zipCode)
    {
        var client = _httpClientFactory.CreateClient();

        // 1. Geocoding: Zipcode -> Lat/Lon (using zippopotam.us)
        // Note: Using http for zippopotam.us as per original implementation
        var geoResponse = await client.GetAsync($"http://api.zippopotam.us/us/{zipCode}");
        if (!geoResponse.IsSuccessStatusCode)
        {
            return null;
        }

        var geoData = await geoResponse.Content.ReadFromJsonAsync<ZipCodeResponse>();
        if (geoData == null || geoData.Places.Count == 0)
        {
            return null;
        }

        var place = geoData.Places[0];
        var lat = place.Latitude;
        var lon = place.Longitude;

        // 2. Weather: Lat/Lon -> Weather (using open-meteo.com)
        var weatherResponse = await client.GetAsync($"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current_weather=true&temperature_unit=fahrenheit");
        if (!weatherResponse.IsSuccessStatusCode)
        {
            // Could throw exception or return null depending on desired error handling
            return null;
        }

        var weatherData = await weatherResponse.Content.ReadFromJsonAsync<OpenMeteoResponse>();
        if (weatherData == null)
        {
            return null;
        }

        var activity = System.Diagnostics.Activity.Current;
        if (activity != null)
        {
            activity.SetTag("weather.zipcode", zipCode);
            activity.SetTag("weather.city", place.PlaceName);
            activity.SetTag("weather.state", place.StateAbbreviation);
            activity.SetTag("weather.temperature_f", (int)weatherData.CurrentWeather.Temperature);
        }

        return new WeatherForecast(
            place.PlaceName,
            place.StateAbbreviation,
            zipCode,
            (int)weatherData.CurrentWeather.Temperature,
            GetWeatherSummary(weatherData.CurrentWeather.WeatherCode),
            DateTime.Now.ToString("yyyy-MM-dd")
        );
    }

    private static string GetWeatherSummary(int code) => code switch
    {
        0 => "Clear sky",
        1 or 2 or 3 => "Mainly clear, partly cloudy, and overcast",
        45 or 48 => "Fog",
        51 or 53 or 55 => "Drizzle",
        61 or 63 or 65 => "Rain",
        71 or 73 or 75 => "Snow fall",
        77 => "Snow grains",
        80 or 81 or 82 => "Rain showers",
        85 or 86 => "Snow showers",
        95 => "Thunderstorm",
        96 or 99 => "Thunderstorm with slight and heavy hail",
        _ => "Unknown"
    };
}
