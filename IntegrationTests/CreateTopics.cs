using Confluent.Kafka;
using Confluent.Kafka.Admin;
using FluentAssertions;
using Xunit;

namespace IntegrationTests;

/// <summary>
/// Ensures required Kafka topics exist in the cluster.
/// </summary>
public class CreateTopics
{
    [Fact]
    public async Task CreateTopics_ShouldSucceed()
    {
        // Arrange
        var config = TestConfig.KafkaAdminConfig;
        using var adminClient = new AdminClientBuilder(config).Build();

        var topics = new[] { "weather-weather-alerts", "weather-general-events" };
        var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
        
        var existingTopics = metadata.Topics.Select(t => t.Topic).ToList();
        var topicsToCreate = topics.Where(t => !existingTopics.Contains(t)).ToList();

        if (topicsToCreate.Any())
        {
            var topicSpecifications = topicsToCreate.Select(t => new TopicSpecification
            {
                Name = t,
                NumPartitions = 1,
                ReplicationFactor = 3 // Confluent Cloud default minimum for many clusters
            });

            // Act
            await adminClient.CreateTopicsAsync(topicSpecifications);
        }

        // Assert
        var finalMetadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
        var finalTopics = finalMetadata.Topics.Select(t => t.Topic).ToList();
        
        foreach (var topic in topics)
        {
            finalTopics.Should().Contain(topic, $"Topic {topic} should exist.");
        }
    }
}
