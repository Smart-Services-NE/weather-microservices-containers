using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Configure OpenTelemetry for distributed tracing
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
    });

// Add services to the container.
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.MapGet("/weatherforecast", async (string? zipcode, IHttpClientFactory httpClientFactory) =>
{
    zipcode ??= "90210"; // Default
    var client = httpClientFactory.CreateClient();

    // 1. Geocoding: Zipcode -> Lat/Lon (using zippopotam.us)
    var geoResponse = await client.GetAsync($"http://api.zippopotam.us/us/{zipcode}");
    if (!geoResponse.IsSuccessStatusCode) return Results.NotFound("Zipcode not found");

    var geoData = await geoResponse.Content.ReadFromJsonAsync<ZipCodeResponse>();
    if (geoData == null || geoData.Places.Count == 0) return Results.NotFound("Invalid zip data");

    var place = geoData.Places[0];
    var lat = place.Latitude;
    var lon = place.Longitude;

    // 2. Weather: Lat/Lon -> Weather (using open-meteo.com)
    var weatherResponse = await client.GetAsync($"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current_weather=true&temperature_unit=fahrenheit");
    if (!weatherResponse.IsSuccessStatusCode) return Results.Problem("Weather service unavailable");

    var weatherData = await weatherResponse.Content.ReadFromJsonAsync<OpenMeteoResponse>();
    if (weatherData == null) return Results.Problem("Invalid weather data");

    return Results.Ok(new
    {
        City = place.PlaceName,
        State = place.StateAbbreviation,
        ZipCode = zipcode,
        TemperatureF = (int)weatherData.CurrentWeather.Temperature,
        Summary = GetWeatherSummary(weatherData.CurrentWeather.WeatherCode),
        Date = DateTime.Now.ToString("yyyy-MM-dd")
    });
});

app.Run();

string GetWeatherSummary(int code) => code switch
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

public record ZipCodeResponse(
    [property: System.Text.Json.Serialization.JsonPropertyName("post code")] string PostCode,
    [property: System.Text.Json.Serialization.JsonPropertyName("places")] List<Place> Places);

public record Place(
    [property: System.Text.Json.Serialization.JsonPropertyName("place name")] string PlaceName,
    [property: System.Text.Json.Serialization.JsonPropertyName("longitude")] string Longitude,
    [property: System.Text.Json.Serialization.JsonPropertyName("state abbreviation")] string StateAbbreviation,
    [property: System.Text.Json.Serialization.JsonPropertyName("latitude")] string Latitude);

public record OpenMeteoResponse(
    [property: System.Text.Json.Serialization.JsonPropertyName("current_weather")] CurrentWeather CurrentWeather);

public record CurrentWeather(
    [property: System.Text.Json.Serialization.JsonPropertyName("temperature")] double Temperature,
    [property: System.Text.Json.Serialization.JsonPropertyName("weathercode")] int WeatherCode);
