using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

// Configure OpenTelemetry for distributed tracing
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddSource("WeatherWeb")
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("weather-web"))
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
builder.Services.AddRazorPages();

// Configure HttpClient to call the WeatherApi microservice
builder.Services.AddHttpClient("WeatherApi", client =>
{
    // In Docker Compose, we'll use the service name as the hostname
    var apiHost = builder.Configuration["WeatherApiHost"] ?? "http://weather-api:8080";
    client.BaseAddress = new Uri(apiHost);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.MapPrometheusScrapingEndpoint();

app.Run();
