# NotificationService Monitoring & Observability

Complete guide for monitoring, logging, tracing, and troubleshooting the NotificationService.

## Quick Access

| Service | URL | Purpose |
|---------|-----|---------|
| **Health Check** | http://localhost:8082/health | Service health status |
| **Metrics** | http://localhost:8082/metrics | Prometheus metrics endpoint |
| **Zipkin Traces** | http://localhost:9411 | Distributed tracing UI |
| **Prometheus** | http://localhost:9090 | Metrics query UI |
| **Grafana** | http://localhost:3000 | Metrics dashboards |

## Observability Stack

The service implements full observability using:
- **Structured Logging** via Microsoft.Extensions.Logging
- **Distributed Tracing** via OpenTelemetry with Zipkin
- **Metrics** via OpenTelemetry with Prometheus

## Logging

### Log Levels

| Level | Usage | Examples |
|-------|-------|----------|
| **Information** | Normal operations | Message received, Email sent, Record created |
| **Warning** | Recoverable issues | Invalid message, Duplicate detected |
| **Error** | Failures requiring attention | SMTP errors, Database errors, Exceptions |
| **Debug** | Detailed troubleshooting | Kafka partition/offset details |

### Viewing Logs

```bash
# All logs
podman compose logs -f notification-api

# Last 100 lines
podman compose logs --tail=100 notification-api

# Only errors
podman compose logs notification-api | grep -i error

# Warnings and errors
podman compose logs notification-api | grep -iE "warn|error"

# Track specific message
podman compose logs notification-api | grep "MessageId=test-001"
```

### Log Patterns

**Successful Processing**:
```
[Information] Received message from topic weather-alerts: MessageId=alert-001
[Information] Created notification record with ID 12345 for message alert-001
[Information] Email sent successfully to user@example.com
[Information] Notification sent successfully: MessageId=alert-001, Recipient=user@example.com
```

**SMTP Failure**:
```
[Error] SMTP error sending email to user@example.com
System.Net.Mail.SmtpException: The SMTP server requires a secure connection
[Error] Failed to send email after retries: MessageId=alert-001
```

**Kafka Issue**:
```
[Error] Error consuming message from Kafka
Confluent.Kafka.ConsumeException: Group authorization failed
```

## Distributed Tracing

### Accessing Traces

**Zipkin UI**: http://localhost:9411

**Search Criteria**:
- Service Name: `NotificationService`
- Span Name: `ProcessAndSendNotification`, `RetryFailedNotification`
- Tags: `topic`, `message.id`, `message.recipient`

### Trace Timeline

```
NotificationService: ProcessAndSendNotification [150ms]
  ├─ ValidateMessage [2ms]
  ├─ GetByMessageIdAsync (Database) [10ms]
  ├─ CreateAsync (Database) [15ms]
  ├─ BuildEmailRequest [1ms]
  ├─ SendEmailAsync (SMTP) [110ms]
  └─ UpdateAsync (Database) [12ms]
```

### Activity Tags

All traces include these tags:
- `topic` - Kafka topic name
- `message.id` - Message identifier
- `message.recipient` - Email recipient

## Metrics

### Custom Notification Metrics

```promql
# Successfully sent notifications (rate per second)
rate(notification_sent_total[5m])

# Failed notifications (rate per second)
rate(notification_failed_total[5m])

# Error rate percentage
rate(notification_failed_total[5m]) / rate(notification_sent_total[5m]) * 100

# Retry success rate
rate(notification_retry_success_total[5m])
```

### HTTP Metrics

```promql
# HTTP request duration (p95)
histogram_quantile(0.95, http_server_request_duration_seconds_bucket{job="notification-api"})

# HTTP request rate
rate(http_server_request_duration_seconds_count{job="notification-api"}[5m])

# HTTP error rate (5xx)
rate(http_server_request_duration_seconds_count{job="notification-api",status=~"5.."}[5m])
```

### Viewing Metrics

**Prometheus UI**: http://localhost:9090
- Query: `notification_sent_total`
- Query: `notification_failed_total`

**Grafana**: http://localhost:3000
- Create dashboards for notification metrics
- Pre-configured ASP.NET Core dashboards available

## Health Checks

### Endpoint

```bash
curl http://localhost:8082/health
```

### Healthy Response

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0234567",
  "entries": {
    "sqlite": {
      "status": "Healthy",
      "description": "SQLite database is responsive"
    }
  }
}
```

### Unhealthy Response

```json
{
  "status": "Unhealthy",
  "entries": {
    "sqlite": {
      "status": "Unhealthy",
      "description": "Unable to connect to database"
    }
  }
}
```

## Alert Indicators

### Critical Issues (Immediate Action)

| Indicator | Log Pattern | Action |
|-----------|-------------|--------|
| Service down | No health check response | `podman compose ps notification-api` |
| Kafka disconnected | `Group authorization failed` | Verify Confluent Cloud ACL permissions |
| Database errors | `Database error` | Check SQLite file permissions, disk space |
| SMTP failures | `SMTP error` | Verify email configuration, credentials |

### Warning Issues (Monitor Closely)

| Indicator | Log Pattern | Action |
|-----------|-------------|--------|
| High retry rate | `Failed to send email after retries` | Check SMTP server health |
| Invalid messages | `Invalid message received` | Check message format from producers |
| Duplicate messages | `Duplicate message detected` | Normal for idempotency, but high rate indicates issue |

## Performance Baselines

| Metric | Expected Value | Alert Threshold |
|--------|---------------|-----------------|
| Processing latency | < 2s | > 5s |
| Error rate | < 1% | > 5% |
| SMTP latency | < 1s | > 3s |
| Database write latency | < 50ms | > 200ms |
| Kafka lag | < 10 messages | > 1000 messages |

## Troubleshooting

### Service Not Starting

```bash
podman compose build --no-cache notification-api
podman compose up -d notification-api
podman compose logs notification-api | head -50
```

### High Error Rate

```bash
# Check SMTP configuration
podman compose logs notification-api | grep "Email__"

# Test SMTP connectivity
podman compose exec notification-api ping smtp.gmail.com
```

### Kafka Connection Issues

```bash
# Verify environment variables
podman compose exec notification-api env | grep KAFKA

# Check connectivity
podman compose logs notification-api | grep "Bootstrap"
```

### Database Issues

```bash
# Check database file
podman compose exec notification-api ls -l /app/data/

# Remove corrupted database (recreates on restart)
rm notification-data/notifications.db
podman compose restart notification-api
```

### No Traces in Zipkin

1. Check Zipkin endpoint in appsettings.json
2. Test connectivity: `curl http://zipkin:9411`
3. Verify OpenTelemetry configuration in Program.cs

### No Metrics in Prometheus

1. Check Prometheus scrape config in prometheus.yml
2. Test endpoint: `curl http://localhost:8082/metrics`
3. Check Prometheus targets: http://localhost:9090/targets

## Diagnostic Data Collection

Before escalating an issue, collect:

```bash
# Service logs (last 500 lines)
podman compose logs --tail=500 notification-api > notification-logs.txt

# Current metrics
curl http://localhost:8082/metrics > notification-metrics.txt

# Health status
curl http://localhost:8082/health > notification-health.json

# Recent errors
podman compose logs notification-api | grep -i error | tail -50 > recent-errors.txt
```

## Configuration

### appsettings.json - Logging

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning",
      "NotificationService": "Information",
      "Confluent.Kafka": "Information"
    }
  }
}
```

### appsettings.json - Zipkin

```json
{
  "Zipkin": {
    "Endpoint": "http://zipkin:9411/api/v2/spans"
  }
}
```

## Alert Rules (Prometheus)

### High Error Rate

```yaml
alert: HighNotificationFailureRate
expr: rate(notification_failed_total[5m]) > 0.1
for: 5m
severity: warning
```

### Service Down

```yaml
alert: NotificationServiceDown
expr: up{job="notification-api"} == 0
for: 1m
severity: critical
```

## Related Documentation

- [Kafka Troubleshooting](../kafka/troubleshooting.md)
- [Confluent Cloud Setup](../kafka/confluent-cloud-setup.md)
- [Debugging Guide](../getting-started/debugging.md)
