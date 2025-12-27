using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using WeatherService.Models;
using WeatherService.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Configure OpenTelemetry for distributed tracing and metrics
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddSource("WeatherApi")
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

// Add services to the container.
builder.Services.AddHttpClient();
builder.Services.AddHybridCache();
builder.Services.AddScoped<IWeatherRepository, WeatherRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.MapGet("/weatherforecast", async (string? zipcode, IWeatherRepository repository) =>
{
    zipcode ??= "68136"; // Default
    
    var forecast = await repository.GetForecastAsync(zipcode);
    
    if (forecast == null)
    {
        return Results.NotFound("Weather data not found for the given zipcode.");
    }
    
    return Results.Ok(forecast);
});

app.MapPrometheusScrapingEndpoint();

app.Run();

