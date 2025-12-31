using NotificationService.Accessors;
using NotificationService.Api;
using NotificationService.Managers;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDaprClient();

builder.Services.AddNotificationServiceManagers(builder.Configuration);

builder.Services.AddControllers().AddDapr();

builder.Services.AddHostedService<KafkaBackgroundService>();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("NotificationService"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("NotificationService")
        .AddZipkinExporter(options =>
        {
            options.Endpoint = new Uri(builder.Configuration["Zipkin:Endpoint"]
                ?? "http://zipkin:9411/api/v2/spans");
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddPrometheusExporter());

builder.Services.AddHealthChecks()
    .AddSqlite(builder.Configuration.GetConnectionString("NotificationDb") ?? "Data Source=notifications.db");

builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    dbContext.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCloudEvents();

app.MapPrometheusScrapingEndpoint();

app.MapHealthChecks("/health");

app.MapSubscribeHandler();

app.MapControllers();

app.MapGet("/api/notifications/pending", async (NotificationService.Contracts.INotificationStorageAccessor storage) =>
{
    var pendingNotifications = await storage.GetPendingRetriesAsync();
    return Results.Ok(pendingNotifications);
});

app.MapPost("/api/notifications/{id}/retry", async (Guid id, NotificationService.Contracts.INotificationManager manager) =>
{
    var result = await manager.RetryFailedNotificationAsync(id);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});

app.Run();
