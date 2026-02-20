using WeatherService.Contracts;

namespace WeatherService.Managers;

public class WeatherManager : IWeatherManager
{
    private readonly IGeoCodingAccessor _geoCoding;
    private readonly IWeatherDataAccessor _weatherData;
    private readonly IWeatherCodeEngine _weatherCode;
    private readonly IWeatherAlertEngine _alertEngine;
    private readonly IAlertPublisherAccessor _alertPublisher;
    private readonly ICacheUtility _cache;
    private readonly ITelemetryUtility _telemetry;

    public WeatherManager(
        IGeoCodingAccessor geoCoding,
        IWeatherDataAccessor weatherData,
        IWeatherCodeEngine weatherCode,
        IWeatherAlertEngine alertEngine,
        IAlertPublisherAccessor alertPublisher,
        ICacheUtility cache,
        ITelemetryUtility telemetry)
    {
        _geoCoding = geoCoding;
        _weatherData = weatherData;
        _weatherCode = weatherCode;
        _alertEngine = alertEngine;
        _alertPublisher = alertPublisher;
        _cache = cache;
        _telemetry = telemetry;
    }

    public async Task<WeatherForecastResult> GetWeatherForecastAsync(string zipCode)
    {
        using var activity = _telemetry.StartActivity("GetWeatherForecast");
        _telemetry.SetTag("weather.zipcode", zipCode);

        var cacheKey = $"weather:{zipCode}";
        var cached = await _cache.GetAsync<WeatherForecastData>(cacheKey);

        if (cached != null)
        {
            _telemetry.SetTag("weather.source", "cache");
            _telemetry.SetTag("weather.city", cached.City);
            _telemetry.SetTag("weather.state", cached.State);
            _telemetry.SetTag("weather.temperature_f", cached.TemperatureF);
            return new WeatherForecastResult(true, cached, null);
        }

        _telemetry.SetTag("weather.source", "upstream");

        var geoResult = await _geoCoding.GetLocationByZipCodeAsync(zipCode);
        if (!geoResult.Success)
        {
            return new WeatherForecastResult(false, null, geoResult.Error);
        }

        var weatherResult = await _weatherData.GetCurrentWeatherAsync(
            geoResult.Latitude!,
            geoResult.Longitude!
        );

        if (!weatherResult.Success)
        {
            return new WeatherForecastResult(false, null, weatherResult.Error);
        }

        var summary = _weatherCode.TranslateWeatherCode(weatherResult.WeatherCode!.Value);

        var hourlyForecasts = weatherResult.HourlyForecasts?.Select(h => h with
        {
            Summary = _weatherCode.TranslateWeatherCode(h.WeatherCode)
        }).ToList();

        var dailyForecasts = weatherResult.DailyForecasts?.Select(d => d with
        {
            Summary = _weatherCode.TranslateWeatherCode(d.WeatherCode)
        }).ToList();

        var forecastData = new WeatherForecastData(
            geoResult.City!,
            geoResult.State!,
            zipCode,
            (int)weatherResult.TemperatureF!.Value,
            summary,
            DateTime.UtcNow.ToString("yyyy-MM-dd"),
            new GeoLocation(geoResult.Latitude!, geoResult.Longitude!),
            hourlyForecasts,
            dailyForecasts
        );

        await _cache.SetAsync(cacheKey, forecastData, TimeSpan.FromMinutes(5));

        _telemetry.SetTag("weather.city", forecastData.City);
        _telemetry.SetTag("weather.state", forecastData.State);
        _telemetry.SetTag("weather.temperature_f", forecastData.TemperatureF);

        return new WeatherForecastResult(true, forecastData, null);
    }

    public async Task<LocationValidationResult> ValidateLocationAsync(string zipCode)
    {
        using var activity = _telemetry.StartActivity("ValidateLocation");
        _telemetry.SetTag("validation.zipcode", zipCode);

        var geoResult = await _geoCoding.GetLocationByZipCodeAsync(zipCode);

        if (!geoResult.Success)
        {
            return new LocationValidationResult(false, null, null, geoResult.Error);
        }

        return new LocationValidationResult(
            true,
            geoResult.City,
            geoResult.State,
            null
        );
    }

    public async Task<WeatherForecastResult?> GetCachedForecastAsync(string zipCode)
    {
        using var activity = _telemetry.StartActivity("GetCachedForecast");
        _telemetry.SetTag("weather.zipcode", zipCode);

        var cacheKey = $"weather:{zipCode}";
        var cached = await _cache.GetAsync<WeatherForecastData>(cacheKey);

        if (cached == null)
        {
            _telemetry.SetTag("cache.hit", false);
            return null;
        }

        _telemetry.SetTag("cache.hit", true);
        return new WeatherForecastResult(true, cached, null);
    }

    public async Task<Result> NotifyIfFreezingAsync(string zipCode, string email, CancellationToken ct)
    {
        using var activity = _telemetry.StartActivity("NotifyIfFreezing");
        _telemetry.SetTag("alert.zipcode", zipCode);
        _telemetry.SetTag("alert.email", email);

        // 1. Get coordinates
        var geoResult = await _geoCoding.GetLocationByZipCodeAsync(zipCode);
        if (!geoResult.Success)
            return new Result(false, Error: geoResult.Error);

        // 2. Get current weather
        var weatherResult = await _weatherData.GetCurrentWeatherAsync(
            geoResult.Latitude!, geoResult.Longitude!);

        if (!weatherResult.Success)
            return new Result(false, Error: weatherResult.Error);

        // 3. Check if freezing (using Engine)
        var temperatureC = (weatherResult.TemperatureF! - 32) * 5 / 9;
        if (_alertEngine.IsFreezing(temperatureC.Value))
        {
            _telemetry.SetTag("alert.sent", true);
            // 4. Publish Alert
            return await _alertPublisher.PublishFreezingAlertAsync(
                email, zipCode, temperatureC.Value, ct);
        }

        _telemetry.SetTag("alert.sent", false);
        return new Result(true);
    }
}
