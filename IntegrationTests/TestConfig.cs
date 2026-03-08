using Microsoft.Extensions.Configuration;
using Confluent.Kafka;

namespace IntegrationTests;

/// <summary>
/// Provides configuration settings for the integration tests.
/// </summary>
public static class TestConfig
{
    private static IConfiguration? _configuration;
    private static string? _projectRoot;

    /// <summary>
    /// Gets the configuration built from appsettings.json.
    /// </summary>
    public static IConfiguration Configuration
    {
        get
        {
            if (_configuration == null)
            {
                _configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .Build();
            }
            return _configuration;
        }
    }

    /// <summary>
    /// Gets the absolute path to the project root (solution directory).
    /// </summary>
    public static string ProjectRoot
    {
        get
        {
            if (_projectRoot == null)
            {
                var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
                while (currentDir != null && !File.Exists(Path.Combine(currentDir.FullName, "WeatherApp.sln")))
                {
                    currentDir = currentDir.Parent;
                }
                _projectRoot = currentDir?.FullName ?? throw new Exception("Could not find project root (solution file).");
            }
            return _projectRoot;
        }
    }

    /// <summary>
    /// Gets the Kafka admin configuration.
    /// </summary>
    public static AdminClientConfig KafkaAdminConfig
    {
        get
        {
            var config = new AdminClientConfig
            {
                BootstrapServers = Configuration["IntegrationTests:Kafka:BootstrapServers"] ?? "localhost:9092"
            };

            var securityProtocol = Configuration["IntegrationTests:Kafka:SecurityProtocol"];
            if (!string.IsNullOrEmpty(securityProtocol) && Enum.TryParse<SecurityProtocol>(securityProtocol, true, out var protocol))
            {
                config.SecurityProtocol = protocol;
            }

            var saslMechanism = Configuration["IntegrationTests:Kafka:SaslMechanism"];
            if (!string.IsNullOrEmpty(saslMechanism) && Enum.TryParse<SaslMechanism>(saslMechanism, true, out var mechanism))
            {
                config.SaslMechanism = mechanism;
            }

            var saslUsername = Configuration["IntegrationTests:Kafka:SaslUsername"];
            if (!string.IsNullOrEmpty(saslUsername))
            {
                config.SaslUsername = saslUsername;
            }

            var saslPassword = Configuration["IntegrationTests:Kafka:SaslPassword"];
            if (!string.IsNullOrEmpty(saslPassword))
            {
                config.SaslPassword = saslPassword;
            }

            return config;
        }
    }
    
    /// <summary>
    /// Gets the absolute path to the notification SQLite database file.
    /// </summary>
    public static string NotificationDbPath
    {
        get
        {
            var relativePath = Configuration["IntegrationTests:Database:NotificationDbPath"] ?? "notification-data/notifications.db";
            // If the path starts with ../, we treat it as relative to the solution root for simplicity in this helper
            if (relativePath.StartsWith("../"))
            {
                 return Path.GetFullPath(Path.Combine(ProjectRoot, relativePath.Substring(3)));
            }
            return Path.GetFullPath(Path.Combine(ProjectRoot, relativePath));
        }
    }
}
