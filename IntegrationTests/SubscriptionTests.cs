using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NotificationService.Accessors;
using NotificationService.Contracts;
using WeatherService.Contracts;
using Xunit;

namespace IntegrationTests;

/// <summary>
/// Integration tests for the Weather Subscription and Sentinel flow.
/// </summary>
public class SubscriptionTests
{
    private readonly HttpClient _httpClient;
    private readonly string _notificationDbPath;

    public SubscriptionTests()
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(TestConfig.Configuration["IntegrationTests:ServiceUrls:WeatherApi"] ?? "http://localhost:8080") };
        _notificationDbPath = TestConfig.NotificationDbPath;

        // Clean up database before each test
        CleanupDatabase();
    }

    private void CleanupDatabase()
    {
        var optionsBuilder = new DbContextOptionsBuilder<NotificationDbContext>();
        optionsBuilder.UseSqlite($"Data Source={_notificationDbPath}");
        using var context = new NotificationDbContext(optionsBuilder.Options);
        context.Notifications.RemoveRange(context.Notifications);
        context.SaveChanges();
    }

    /// <summary>
    /// Verifies that creating a subscription and processing it results in a notification.
    /// </summary>
    [Fact]
    public async Task SubscriptionFlow_ShouldPropagateToNotificationDatabase()
    {
        // Arrange
        var testEmail = $"sub-test-{Guid.NewGuid()}@example.com";
        var zipCode = "99701"; // Fairbanks, AK (Cold)
        // Set threshold to 50F (likely to be triggered in Fairbanks)
        var request = new SubscriptionRequest(testEmail, zipCode, ThresholdType.TemperatureLow, 50.0, "less-than");

        // Act
        // 1. Create Subscription
        var subResponse = await _httpClient.PostAsJsonAsync("/api/weather/subscriptions", request);
        subResponse.EnsureSuccessStatusCode();

        // 2. Process Subscriptions
        var processResponse = await _httpClient.PostAsync("/api/weather/subscriptions/process", null);
        processResponse.EnsureSuccessStatusCode();

        // Assert - Poll the database for the notification
        var optionsBuilder = new DbContextOptionsBuilder<NotificationDbContext>();
        optionsBuilder.UseSqlite($"Data Source={_notificationDbPath}");

        NotificationRecord? notification = null;
        var timeout = TimeSpan.FromSeconds(30);
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < timeout)
        {
            using var context = new NotificationDbContext(optionsBuilder.Options);
            notification = await context.Notifications
                .FirstOrDefaultAsync(n => n.Recipient == testEmail);

            if (notification != null)
                break;

            await Task.Delay(1000);
        }

        notification.Should().NotBeNull($"A sentinel notification for {testEmail} should have been created.");
        notification!.Subject.Should().Contain("Low Temperature Alert");
        notification.Recipient.Should().Be(testEmail);
    }
}
