# ContainerApp

Cloud-native .NET 10 microservices application with Dapr, Kafka, and full observability.

## Quick Start

```bash
# Start all services
podman compose up -d --build

# Access services
open http://localhost:8081  # WeatherWeb (Frontend)
open http://localhost:9411  # Zipkin (Traces)
open http://localhost:3000  # Grafana (admin/admin)
```

## Architecture

### Services

- **WeatherWeb** (Port 8081) - Razor Pages frontend
- **WeatherService** (Port 8080) - Weather forecast API with HybridCache
- **NotificationService** (Port 8082) - Kafka consumer + email notifications

### Infrastructure

- **Dapr** - Service invocation and resiliency
- **Kafka** - Message streaming (local or Confluent Cloud)
- **Zipkin** - Distributed tracing (Port 9411)
- **Prometheus** - Metrics collection (Port 9090)
- **Grafana** - Metrics visualization (Port 3000)

## Key Features

- **IDesign Architecture** - Layered architecture with volatility encapsulation
- **HybridCache** - .NET 10 caching with stampede protection (5-minute TTL)
- **Distributed Tracing** - OpenTelemetry with Zipkin spans
- **Avro Support** - Automatic JSON/Avro deserialization with Schema Registry
- **Observability** - Structured logging, metrics, and health checks

## Documentation

📚 **[Complete Documentation](./docs/README.md)**

### Quick Links

- **[Quick Start Guide](./docs/getting-started/quick-start.md)** - Detailed setup instructions
- **[Debugging Guide](./docs/getting-started/debugging.md)** - Debug containerized apps
- **[Kafka Setup](./docs/kafka/confluent-cloud-setup.md)** - Configure Confluent Cloud
- **[Avro Setup](./docs/kafka/avro-setup.md)** - Schema Registry configuration
- **[Troubleshooting](./docs/kafka/troubleshooting.md)** - Common issues and solutions
- **[Monitoring](./docs/services/notification-service.md)** - Service observability
- **[Message Schemas](./docs/infrastructure/schemas.md)** - Avro schema documentation
- **[Project Architecture](./CLAUDE.md)** - IDesign principles and guidelines

## Common Tasks

### Test Weather Service

```bash
# Get weather forecast
curl "http://localhost:8080/api/weather/forecast?zipcode=94105"

# Via frontend
open http://localhost:8081
```

### Send Test Notification

**Via Confluent Cloud Console** (easiest):
1. Go to https://confluent.cloud
2. Topics → `weather-alerts` → Produce message
3. Format: String (JSON) or Avro
4. Paste message:
```json
{
  "messageId": "test-001",
  "subject": "Test Notification",
  "body": "Testing notification system",
  "recipient": "your-email@example.com",
  "timestamp": 1735574400000
}
```

**Via Python Script**:
```bash
python3 scripts/produce-weather-alerts.py --severity CRITICAL
```

### Monitor Services

```bash
# View logs
podman compose logs -f notification-api

# Check health
curl http://localhost:8082/health

# View metrics
curl http://localhost:8082/metrics
```

## Project Structure

```
containerApp/
├── WeatherWeb/              # Frontend Razor Pages
├── WeatherService/          # Weather API microservice
│   ├── Contracts/           # Interfaces and DTOs
│   ├── Managers/            # Orchestration layer
│   ├── Engines/             # Business logic
│   ├── Accessors/           # External API access
│   └── Utilities/           # Caching, telemetry
├── NotificationService/     # Kafka consumer + email sender
│   ├── Contracts/           # Interfaces and DTOs
│   ├── Managers/            # Orchestration layer
│   ├── Engines/             # Message validation
│   ├── Accessors/           # Email, database access
│   └── Utilities/           # Kafka consumer, retry policy
├── docs/                    # Documentation
│   ├── getting-started/     # Quick start, debugging
│   ├── kafka/               # Confluent Cloud, Avro, troubleshooting
│   ├── services/            # Service monitoring
│   ├── infrastructure/      # Schemas, Azure Storage
│   └── scripts/             # Producer scripts
├── schemas/avro/            # Avro schemas
├── scripts/                 # Python producer scripts
├── compose.yaml             # Service orchestration (Podman native)
├── .env.example             # Environment variables template
└── CLAUDE.md                # Project architecture guidelines
```

## Technology Stack

- **.NET 10.0** - Latest .NET runtime
- **ASP.NET Core** - Web framework
- **Dapr 1.14** - Service mesh
- **Kafka (Confluent)** - Message streaming
- **OpenTelemetry** - Observability
- **Zipkin** - Distributed tracing
- **Prometheus** - Metrics
- **Grafana** - Visualization
- **SQLite** - Local persistence
- **Entity Framework Core** - ORM
- **HybridCache** - Microsoft's new caching library
- **Polly** - Resilience and retry policies

## Configuration

### Environment Variables

Create `.env` from `.env.example`:

```bash
# Confluent Cloud Kafka
KAFKA_BOOTSTRAP_SERVERS=pkc-xxxxx.confluent.cloud:9092
KAFKA_SASL_USERNAME=YOUR_API_KEY
KAFKA_SASL_PASSWORD=YOUR_API_SECRET

# Schema Registry (optional, for Avro)
KAFKA_SCHEMA_REGISTRY_URL=https://psrc-xxxxx.confluent.cloud
KAFKA_SCHEMA_REGISTRY_KEY=YOUR_SR_KEY
KAFKA_SCHEMA_REGISTRY_SECRET=YOUR_SR_SECRET
```

### Email Configuration

Edit `NotificationService/Clients/WebApi/appsettings.json`:

```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "EnableSsl": true,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password"
  }
}
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Services won't start | `podman compose down -v && podman compose up -d --build` |
| Port already in use | `lsof -i :8080` then `kill -9 <PID>` |
| Kafka auth failed | Check `.env` credentials |
| 0x00 JSON error | [See troubleshooting guide](./docs/kafka/troubleshooting.md#message-format-issues) |
| No email sent | Check SMTP config in `appsettings.json` |

📖 **[Full Troubleshooting Guide](./docs/kafka/troubleshooting.md)**

## Development

### Run Tests

```bash
# Run unit tests
dotnet test

# Run end-to-end integration tests
dotnet test IntegrationTests/IntegrationTests.csproj
```

### Debug Service

```bash
# Rebuild in debug mode
podman compose -f docker-compose.yml -f docker-compose.debug.yml up -d --build notification-api

# In VS Code: F5 → "Podman: Attach to NotificationService"
```

📖 **[Debugging Guide](./docs/getting-started/debugging.md)**

## Architecture Principles

This project follows **IDesign Architecture**:

- **Closed Architecture** - Components only call down one layer
- **Volatility-Based Decomposition** - Changes encapsulated by layer
- **No Sideways Calls** - Except queued calls between Managers
- **Managers** orchestrate workflows
- **Engines** contain pure business logic (no I/O)
- **Accessors** shield external resources

📖 **[Complete Architecture Guide](./CLAUDE.md)**

## License

This project is for educational purposes as part of the containerApp microservices demonstration.

## Related Resources

- [IDesign Architecture](http://www.idesign.net/)
- [Dapr Documentation](https://docs.dapr.io/)
- [Confluent Kafka](https://docs.confluent.io/)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)
