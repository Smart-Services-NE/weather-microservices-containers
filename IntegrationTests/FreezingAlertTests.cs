using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NotificationService.Accessors;
using NotificationService.Contracts;
using WeatherService.Contracts;
using Xunit;

namespace IntegrationTests;

/// <summary>
/// Integration tests for the Freezing Alert end-to-end flow.
/// </summary>
public class FreezingAlertTests
{
    private readonly HttpClient _httpClient;
    private readonly string _notificationDbPath;

    public FreezingAlertTests()
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(TestConfig.Configuration["IntegrationTests:ServiceUrls:WeatherApi"] ?? "http://localhost:8080") };
        _notificationDbPath = TestConfig.NotificationDbPath;
    }

    /// <summary>
    /// Verifies that a freezing alert request to WeatherService results in a notification record in the NotificationService database.
    /// </summary>
    [Fact]
    public async Task FreezingAlert_ShouldPropagateToNotificationDatabase()
    {
        // Arrange
        var testEmail = $"test-{Guid.NewGuid()}@example.com";
        var zipCode = "68136"; // Omaha, NE
        var request = new FreezingAlertRequest(zipCode, testEmail);

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/weather/alerts/freezing", request);
        response.EnsureSuccessStatusCode();

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

            await Task.Delay(1000); // Wait 1 second before polling again
        }

        notification.Should().NotBeNull($"A notification for {testEmail} should have been created within {timeout.TotalSeconds} seconds.");
        notification!.Subject.Should().Contain("Freezing");
        notification.Recipient.Should().Be(testEmail);
    }
}
