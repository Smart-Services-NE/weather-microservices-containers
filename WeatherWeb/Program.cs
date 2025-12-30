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

// Add Dapr client for service invocation
// Configure to use HTTP endpoint since Dapr sidecar is in a separate container
builder.Services.AddDaprClient(client =>
{
    client.UseHttpEndpoint("http://weather-web-dapr:3500");
    client.UseGrpcEndpoint("http://weather-web-dapr:50002");
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
