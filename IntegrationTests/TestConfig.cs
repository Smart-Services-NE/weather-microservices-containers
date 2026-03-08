using Microsoft.Extensions.Configuration;

namespace IntegrationTests;

public static class TestConfig
{
    private static IConfiguration? _configuration;
    private static string? _projectRoot;

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

    public static string KafkaBootstrapServers => Configuration["IntegrationTests:Kafka:BootstrapServers"] ?? "localhost:9092";
    
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
