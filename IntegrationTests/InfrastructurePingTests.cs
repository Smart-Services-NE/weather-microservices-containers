using Confluent.Kafka;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NotificationService.Accessors;
using Xunit;

namespace IntegrationTests;

public class InfrastructurePingTests
{
    [Fact]
    public void Kafka_ShouldBeConnectable()
    {
        // Arrange
        var config = new AdminClientConfig
        {
            BootstrapServers = TestConfig.KafkaBootstrapServers
        };

        // Act & Assert
        using var adminClient = new AdminClientBuilder(config).Build();
        var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(5));
        
        metadata.Brokers.Should().NotBeEmpty();
    }

    [Fact]
    public void NotificationDatabase_ShouldBeConnectableAndHaveTables()
    {
        // Arrange
        var dbPath = TestConfig.NotificationDbPath;
        
        var optionsBuilder = new DbContextOptionsBuilder<NotificationDbContext>();
        optionsBuilder.UseSqlite($"Data Source={dbPath}");

        using var context = new NotificationDbContext(optionsBuilder.Options);

        // Act & Assert
        var exists = File.Exists(dbPath);
        exists.Should().BeTrue($"The notification database file should exist at {dbPath}");

        var canConnect = context.Database.CanConnect();
        canConnect.Should().BeTrue($"The notification database should be connectable at {dbPath}");

        // Check if we can query the Notifications table
        var count = context.Notifications.Count();
        count.Should().BeGreaterThanOrEqualTo(0);
    }
}
