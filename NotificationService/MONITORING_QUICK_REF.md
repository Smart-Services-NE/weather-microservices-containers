# NotificationService Monitoring Quick Reference

## 🔍 Quick Access URLs

| Service | URL | Purpose |
|---------|-----|---------|
| **Health Check** | http://localhost:8082/health | Service health status |
| **Metrics** | http://localhost:8082/metrics | Prometheus metrics endpoint |
| **Zipkin Traces** | http://localhost:9411 | Distributed tracing UI |
| **Prometheus** | http://localhost:9090 | Metrics query UI |
| **Grafana** | http://localhost:3000 | Metrics dashboards |

## 📊 Key Metrics to Monitor

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

# Retry failure rate
rate(notification_retry_failed_total[5m])
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

## 🐛 Troubleshooting Commands

### View Real-time Logs
```bash
# All logs
docker-compose logs -f notification-api

# Last 100 lines
docker-compose logs --tail=100 notification-api

# Only errors
docker-compose logs notification-api | grep -i error

# Only warnings and errors
docker-compose logs notification-api | grep -iE "warn|error"

# Specific message ID
docker-compose logs notification-api | grep "MessageId=alert-001"
```

### Check Service Health
```bash
# Health check endpoint
curl http://localhost:8082/health

# Metrics endpoint (should return Prometheus format)
curl http://localhost:8082/metrics

# Docker container status
docker-compose ps notification-api

# Container resource usage
docker stats containerapp-notification-api-1
```

### Kafka Consumer Status
```bash
# Check if consumer is running
docker-compose logs notification-api | grep "Subscribed to topics"

# Expected output:
# Subscribed to Kafka topics: weather-alerts, general-events

# Check for consumer errors
docker-compose logs notification-api | grep -i kafka | grep -i error
```

### Database Status
```bash
# Check SQLite database file
ls -lh notification-data/notifications.db

# View database records via API
curl http://localhost:8082/api/notifications/pending
```

## 🚨 Alert Indicators

### Critical Issues (Immediate Action Required)

| Indicator | Log Pattern | Action |
|-----------|-------------|--------|
| Service down | No health check response | Check container: `docker-compose ps notification-api` |
| Kafka disconnected | `Group authorization failed` | Verify Confluent Cloud ACL permissions |
| Database errors | `Database error` | Check SQLite file permissions and disk space |
| SMTP failures | `SMTP error` | Verify email configuration, credentials |

### Warning Issues (Monitor Closely)

| Indicator | Log Pattern | Action |
|-----------|-------------|--------|
| High retry rate | `Failed to send email after retries` | Check SMTP server health |
| Invalid messages | `Invalid message received` | Check message format from producers |
| Duplicate messages | `Duplicate message detected` | Normal for idempotency, but high rate indicates issue |

## 📝 Common Log Patterns

### Successful Processing
```
[Information] Received message from topic weather-alerts: MessageId=alert-001
[Information] Created notification record with ID 12345 for message alert-001
[Information] Email sent successfully to user@example.com
[Information] Notification sent successfully: MessageId=alert-001, Recipient=user@example.com
[Information] Successfully processed and sent notification for MessageId=alert-001
```

### SMTP Failure
```
[Error] SMTP error sending email to user@example.com
System.Net.Mail.SmtpException: The SMTP server requires a secure connection
[Error] Failed to send email after retries: MessageId=alert-001
[Warning] Failed to process notification for MessageId=alert-001: Failed to send email
```

### Kafka Connection Issue
```
[Error] Error consuming message from Kafka
Confluent.Kafka.ConsumeException: Group authorization failed
```

### Database Issue
```
[Error] Database error creating notification record
Microsoft.EntityFrameworkCore.DbUpdateException: SQLite Error 13: 'database or disk is full'
```

## 📈 Performance Baselines

### Normal Operating Metrics

| Metric | Expected Value | Alert Threshold |
|--------|---------------|-----------------|
| Processing latency | < 2s | > 5s |
| Error rate | < 1% | > 5% |
| SMTP latency | < 1s | > 3s |
| Database write latency | < 50ms | > 200ms |
| Kafka lag | < 10 messages | > 1000 messages |

## 🔧 Quick Fixes

### Service Not Starting
```bash
# Rebuild and restart
docker-compose build --no-cache notification-api
docker-compose up -d notification-api

# Check logs for startup errors
docker-compose logs notification-api | head -50
```

### High Error Rate
```bash
# Check SMTP configuration
docker-compose logs notification-api | grep "Email__"

# Test SMTP connectivity (from container)
docker-compose exec notification-api ping smtp.gmail.com
```

### Kafka Connection Issues
```bash
# Verify environment variables are loaded
docker-compose exec notification-api env | grep KAFKA

# Check Confluent Cloud connectivity
docker-compose logs notification-api | grep "Bootstrap"
```

### Database Issues
```bash
# Check database file exists and is writable
docker-compose exec notification-api ls -l /app/data/

# Remove corrupted database (will recreate on restart)
rm notification-data/notifications.db
docker-compose restart notification-api
```

## 📞 Escalation Checklist

Before escalating an issue, collect:

1. **Service Logs** (last 500 lines):
   ```bash
   docker-compose logs --tail=500 notification-api > notification-logs.txt
   ```

2. **Current Metrics Snapshot**:
   ```bash
   curl http://localhost:8082/metrics > notification-metrics.txt
   ```

3. **Health Status**:
   ```bash
   curl http://localhost:8082/health > notification-health.json
   ```

4. **Recent Traces** (from Zipkin):
   - Navigate to http://localhost:9411
   - Search for service: `NotificationService`
   - Export failing traces

5. **Error Summary**:
   ```bash
   docker-compose logs notification-api | grep -i error | tail -50 > recent-errors.txt
   ```

## 🎯 Health Check Interpretation

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
  "totalDuration": "00:00:05.1234567",
  "entries": {
    "sqlite": {
      "status": "Unhealthy",
      "description": "Unable to connect to database",
      "exception": "Microsoft.Data.Sqlite.SqliteException: ..."
    }
  }
}
```

## 📚 Additional Resources

- **Full Documentation**: [OBSERVABILITY.md](OBSERVABILITY.md)
- **Confluent Cloud Setup**: [QUICK_START_CONFLUENT.md](../QUICK_START_CONFLUENT.md)
- **Sample Payloads**: [samples/notification-message-samples.json](../samples/notification-message-samples.json)
- **Project Architecture**: [CLAUDE.md](../CLAUDE.md)
