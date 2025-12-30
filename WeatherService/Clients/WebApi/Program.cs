using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using WeatherService.Contracts;
using WeatherService.Engines;
using WeatherService.Accessors;
using WeatherService.Managers;
using WeatherService.Utilities;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddSource("WeatherService")
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("weather-api"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddZipkinExporter(options =>
            {
                options.Endpoint = new Uri("http://zipkin:9411/api/v2/spans");
            });
    })
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();

        metrics.AddPrometheusExporter();
    });

builder.Services.AddHttpClient();
builder.Services.AddHybridCache();

builder.Services.AddWeatherServiceManagers();
builder.Services.AddWeatherServiceEngines();
builder.Services.AddWeatherServiceUtilities();

// builder.Services.AddScoped<IWeatherManager, WeatherManager>();
// builder.Services.AddScoped<IGeoCodingAccessor, GeoCodingAccessor>();
// builder.Services.AddScoped<IWeatherDataAccessor, WeatherDataAccessor>();
// builder.Services.AddScoped<IWeatherCodeEngine, WeatherCodeEngine>();
// builder.Services.AddScoped<ICacheUtility, CacheUtility>();
// builder.Services.AddSingleton<ITelemetryUtility, TelemetryUtility>();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/api/weather/forecast", async (string? zipcode, IWeatherManager manager) =>
{
    zipcode ??= "68136";

    var result = await manager.GetWeatherForecastAsync(zipcode);

    if (!result.Success)
    {
        return Results.BadRequest(new { error = result.Error });
    }

    return Results.Ok(result.Data);
});

app.MapGet("/api/weather/validate", async (string zipcode, IWeatherManager manager) =>
{
    var result = await manager.ValidateLocationAsync(zipcode);

    if (!result.IsValid)
    {
        return Results.BadRequest(new { error = result.Error });
    }

    return Results.Ok(new { city = result.City, state = result.State });
});

app.MapGet("/api/weather/cached", async (string zipcode, IWeatherManager manager) =>
{
    var result = await manager.GetCachedForecastAsync(zipcode);

    if (result == null)
    {
        return Results.NotFound(new { message = "No cached data found for this zipcode" });
    }

    return Results.Ok(result.Data);
});

app.MapPrometheusScrapingEndpoint();

app.Run();

public partial class Program { }
