# ContainerApp Documentation

Comprehensive documentation for the containerApp microservices project.

> **For Contributors**: See [Documentation Guidelines](./DOCUMENTATION_GUIDELINES.md) for standards on creating and updating documentation.

## Quick Links

- [Quick Start Guide](./getting-started/quick-start.md) - Get up and running in minutes
- [Project Architecture](../CLAUDE.md) - IDesign architecture principles and guidelines
- [Debugging Guide](./getting-started/debugging.md) - Debug containerized applications

## Documentation Structure

### Getting Started

Essential guides to get the application running.

- **[Quick Start Guide](./getting-started/quick-start.md)**
  - Start all services
  - Access URLs and credentials
  - Basic configuration
  - Common issues

- **[Debugging Guide](./getting-started/debugging.md)**
  - Remote debugging (attach to container)
  - Local debugging (run outside Docker)
  - Console logging
  - Troubleshooting debugger issues

### Kafka Integration

Configure and troubleshoot Kafka message streaming.

- **[Confluent Cloud Setup](./kafka/confluent-cloud-setup.md)**
  - Get credentials
  - Configure environment variables
  - Create topics
  - Test connection

- **[Avro Schema Registry Setup](./kafka/avro-setup.md)**
  - Get Schema Registry credentials
  - Configure Avro deserialization
  - Automatic mode detection
  - Schema compatibility

- **[Kafka Troubleshooting](./kafka/troubleshooting.md)**
  - Message format issues (JSON vs Avro)
  - Connection issues
  - Schema Registry errors
  - Diagnostic commands

### Services

Monitor and troubleshoot individual microservices.

- **[NotificationService Monitoring & Observability](./services/notification-service.md)**
  - Access URLs (health, metrics, traces)
  - Logging patterns
  - Distributed tracing with Zipkin
  - Prometheus metrics
  - Alert indicators
  - Performance baselines

### Infrastructure

Shared infrastructure components and configuration.

- **[Message Schemas](./infrastructure/schemas.md)**
  - notification-message.avsc (generic notifications)
  - weather-alert.avsc (weather-specific alerts)
  - Message formats (JSON vs Avro)
  - Schema validation
  - Publishing examples

- **[Azure Storage with Azurite](./infrastructure/azure-storage.md)**
  - Local Azure Storage emulation
  - Connection strings
  - Table Storage examples
  - Blob and Queue Storage
  - Production migration

### Scripts

Python scripts for testing and automation.

- **[Weather Alert Producer](./scripts/weather-alert-producer.md)**
  - Send test weather alerts
  - Confluent Cloud Console method
  - Python producer script
  - Sample messages (all severity levels)
  - Troubleshooting producer issues

## Common Tasks

### Start the Application

```bash
# Clone and start
cd /Users/ghostair/Projects/containerApp
podman compose up -d --build

# Access services
open http://localhost:8081  # WeatherWeb
open http://localhost:9411  # Zipkin Traces
open http://localhost:3000  # Grafana (admin/admin)
```

### Send Test Notification

**Via Confluent Cloud Console** (easiest):
1. Go to https://confluent.cloud
2. Topics → `general-events` → Produce message
3. Format: String (JSON) or Avro
4. Paste message, click Produce

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

# View traces
open http://localhost:9411
```

### Debug a Service

```bash
# Rebuild in debug mode
podman compose -f podman compose.yml -f podman compose.debug.yml up -d --build notification-api

# In VS Code: F5 → "Docker: Attach to NotificationService"
# Set breakpoints, send test message
```

## Architecture

This project follows **IDesign Architecture** principles:

### Component Layers
1. **Contracts** - Interfaces and DTOs
2. **Managers** - Orchestrate use cases
3. **Engines** - Pure business logic (no I/O)
4. **Accessors** - External resource access
5. **Utilities** - Cross-cutting concerns
6. **Clients/WebApi** - API entry points

### Services
- **WeatherWeb** (Port 8081) - Razor Pages frontend
- **WeatherService** (Port 8080) - Weather API with HybridCache
- **NotificationService** (Port 8082) - Kafka consumer + email sender

### Infrastructure
- **Dapr** - Service invocation, resiliency
- **Kafka** - Message streaming (local or Confluent Cloud)
- **Zipkin** - Distributed tracing
- **Prometheus** - Metrics collection
- **Grafana** - Metrics visualization

## Key Concepts

### Message Flow

```
Producer (Confluent Cloud / Python script)
    ↓
Kafka Topic (weather-alerts / general-events)
    ↓
NotificationService (Consumer)
    ↓
Email via SMTP
    ↓
SQLite Database
```

### Avro vs JSON

- **JSON**: String format, no Schema Registry needed, easy testing
- **Avro**: Binary format, requires Schema Registry, type-safe, smaller size

Service automatically detects and uses correct deserializer.

### Observability

All services implement:
- **Structured Logging** - Correlation via MessageId
- **Distributed Tracing** - OpenTelemetry + Zipkin
- **Metrics** - Prometheus endpoints
- **Health Checks** - `/health` endpoints

## Troubleshooting

### Common Issues

| Issue | Solution |
|-------|----------|
| Services won't start | `podman compose down -v && podman compose up -d --build` |
| Port already in use | `lsof -i :8080` then `kill -9 <PID>` |
| Kafka auth failed | Check `.env` credentials |
| 0x00 JSON error | Send JSON (not Avro) or configure Schema Registry |
| No email sent | Check SMTP config in `appsettings.json` |

### Diagnostic Commands

```bash
# Check all services
podman compose ps

# View logs for specific service
podman compose logs -f notification-api

# Check Kafka connectivity
podman compose exec notification-api env | grep KAFKA

# View database records
curl http://localhost:8082/api/notifications/pending
```

## Additional Resources

- **Project Guidelines**: [../CLAUDE.md](../CLAUDE.md)
- **README**: [../README.md](../README.md)
- **Schemas**: [../schemas/avro/](../schemas/avro/)
- **Scripts**: [../scripts/](../scripts/)

## Contributing

When adding documentation:
1. Follow the existing structure
2. Use clear, concise language
3. Include code examples
4. Link to related documents
5. Test all commands before documenting

## Need Help?

- Check [Troubleshooting guides](./kafka/troubleshooting.md)
- Review [Service logs](./services/notification-service.md#logging)
- Inspect [Traces in Zipkin](http://localhost:9411)
- Open an issue with diagnostic data
