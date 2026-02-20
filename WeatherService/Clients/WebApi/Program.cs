using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using WeatherService.Contracts;
using WeatherService.Engines;
using WeatherService.Accessors;
using WeatherService.Managers;
using Microsoft.EntityFrameworkCore;
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

builder.Services.AddDaprClient();
builder.Services.AddHybridCache();

builder.Services.AddWeatherServiceManagers(builder.Configuration);
builder.Services.AddHealthChecks();


var app = builder.Build();

app.UseHttpsRedirection();

// Ensure Database exists
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WeatherDbContext>();
    db.Database.EnsureCreated();
}

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

app.MapPost("/api/weather/alerts/freezing", async (string zipcode, string email, IWeatherManager manager, CancellationToken ct) =>
{
    var result = await manager.NotifyIfFreezingAsync(zipcode, email, ct);

    if (!result.Success)
    {
        return Results.BadRequest(new { error = result.Error });
    }

    return Results.Ok(new { message = "Freezing alert check completed." });
});

app.MapPost("/api/weather/subscriptions", async (SubscriptionRequest request, ISubscriptionManager manager, CancellationToken ct) =>
{
    var result = await manager.SubscribeAsync(request, ct);
    if (!result.Success)
    {
        return Results.BadRequest(new { error = result.Error });
    }
    return Results.Ok(new { message = "Subscribed successfully." });
});

app.MapPost("/api/weather/subscriptions/process", async (ISubscriptionManager manager, CancellationToken ct) =>
{
    var result = await manager.ProcessSubscriptionsAsync(ct);
    if (!result.Success)
    {
        return Results.InternalServerError(new { error = result.Error });
    }
    return Results.Ok(new { message = "Batch processing completed." });
});

app.MapPost("/cron-check", async (ISubscriptionManager manager, CancellationToken ct) =>
{
    await manager.ProcessSubscriptionsAsync(ct);
    return Results.Ok();
});

app.MapHealthChecks("/health");
app.MapPrometheusScrapingEndpoint();

app.Run();

public partial class Program { }
