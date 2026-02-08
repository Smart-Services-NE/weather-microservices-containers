# Quick Start Guide

Get the containerApp microservices running in minutes.

## Prerequisites

- Podman installed and running
- .NET 10 SDK (for local development)
- Git

## Start All Services

```bash
# Clone repository (if needed)
cd /Users/ghostair/Projects/containerApp

# Start all services
podman compose up -d --build

# Check status
podman compose ps
```

## Access Services

| Service | URL | Credentials |
|---------|-----|-------------|
| **WeatherWeb** | http://localhost:8081 | - |
| **WeatherService** | http://localhost:8080/api/weather | - |
| **NotificationService** | http://localhost:8082/health | - |
| **Zipkin Traces** | http://localhost:9411 | - |
| **Prometheus Metrics** | http://localhost:9090 | - |
| **Grafana Dashboards** | http://localhost:3000 | admin/admin |

## Test Weather Service

```bash
# Get weather forecast for zip code
curl "http://localhost:8080/api/weather/forecast?zipcode=94105"

# Via frontend
open http://localhost:8081
```

## Test Notification Service

### Send Test Message (Kafka)

```bash
# Via Confluent Cloud Console (easiest)
# 1. Go to https://confluent.cloud
# 2. Topics → general-events → Produce message
# 3. Format: String (for JSON) or Avro (if Schema Registry configured)
# 4. Paste message:
{
  "messageId": "test-001",
  "subject": "Test Notification",
  "body": "Testing notification system",
  "recipient": "your-email@example.com",
  "timestamp": 1735574400000,
  "metadata": {
    "from": "test@weatherapp.com",
    "isHtml": "false"
  }
}

# Check logs
podman compose logs -f notification-api | grep "test-001"
```

## Stop Services

```bash
# Stop all services
podman compose down

# Stop and remove volumes (clean state)
podman compose down -v
```

## Architecture Overview

### Services

1. **WeatherWeb** (Port 8081)
   - Frontend Razor Pages application
   - Calls WeatherService via Dapr

2. **WeatherService** (Port 8080)
   - Weather forecast API
   - Integrates with geocoding and weather APIs
   - Uses HybridCache (5-minute TTL)

3. **NotificationService** (Port 8082)
   - Kafka consumer for weather alerts
   - Sends email notifications via SMTP
   - Stores records in SQLite

### Infrastructure

- **Dapr Sidecars**: Service invocation and resiliency
- **Kafka**: Message streaming (local or Confluent Cloud)
- **Zipkin**: Distributed tracing
- **Prometheus**: Metrics collection
- **Grafana**: Metrics visualization

## Configuration

### Using Local Kafka

Default configuration uses local Kafka running in Podman.

```yaml
# podman compose.yml (already configured)
- Kafka__BootstrapServers=kafka:9093
```

### Using Confluent Cloud

See [Confluent Cloud Setup Guide](../kafka/confluent-cloud-setup.md)

1. Create `.env` file with credentials
2. Update `podman compose.yml` environment variables
3. Comment out local kafka/zookeeper services

### Email Configuration

Edit `NotificationService/Clients/WebApi/appsettings.json`:

```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "EnableSsl": true,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "FromAddress": "noreply@weatherapp.com"
  }
}
```

**For Gmail**: Use App Password, not regular password.

## Verify Everything Works

### Check Service Health

```bash
# WeatherService
curl http://localhost:8080/api/weather/validate?zipcode=94105

# NotificationService
curl http://localhost:8082/health

# All services
podman compose ps
```

### Check Logs

```bash
# All services
podman compose logs

# Specific service
podman compose logs -f weather-api
podman compose logs -f notification-api

# Dapr sidecars
podman compose logs -f weather-api-dapr
```

### View Traces

1. Open http://localhost:9411
2. Search for service: `WeatherService` or `NotificationService`
3. View trace timelines

### View Metrics

1. Open http://localhost:9090
2. Query: `http_server_request_duration_seconds`
3. Or browse available metrics at http://localhost:8082/metrics

## Common Issues

### Port Already in Use

```bash
# Check what's using port 8080
lsof -i :8080

# Kill process
kill -9 <PID>
```

### Services Not Starting

```bash
# Check logs
podman compose logs | grep -i error

# Rebuild from scratch
podman compose down -v
podman compose up -d --build
```

### Can't Connect to Kafka

**Local Kafka**:
```bash
# Verify Kafka is running
podman compose ps kafka

# Check logs
podman compose logs kafka
```

**Confluent Cloud**:
- Verify credentials in `.env`
- Check API key permissions
- See [Kafka Troubleshooting](../kafka/troubleshooting.md)

## Next Steps

- [Configure Confluent Cloud](../kafka/confluent-cloud-setup.md)
- [Set up Avro Schema Registry](../kafka/avro-setup.md)
- [Send test weather alerts](../scripts/weather-alert-producer.md)
- [Monitor services](../services/notification-service.md)
- [Debug services](./debugging.md)

## Project Structure

```
containerApp/
├── WeatherWeb/              # Frontend Razor Pages
├── WeatherService/          # Weather API microservice
├── NotificationService/     # Kafka consumer + email sender
├── docs/                    # Documentation
├── schemas/                 # Avro schemas
├── scripts/                 # Python producer scripts
├── podman compose.yml       # Service orchestration (Podman compatible)
└── .env                     # Environment variables (create from .env.example)
```

## Related Documentation

- [Debugging Guide](./debugging.md)
- [Project Architecture](../../CLAUDE.md)
- [NotificationService Monitoring](../services/notification-service.md)
- [Kafka Setup](../kafka/confluent-cloud-setup.md)
