# NotificationService Observability Guide

This document describes the comprehensive logging, tracing, and metrics implemented in the NotificationService.

## Overview

The NotificationService implements full observability using:
- **Structured Logging** via Microsoft.Extensions.Logging
- **Distributed Tracing** via OpenTelemetry with Zipkin
- **Metrics** via OpenTelemetry with Prometheus

## Logging Strategy

### Log Levels Used

| Level | Usage | Examples |
|-------|-------|----------|
| **Information** | Normal operations, successful processing | Message received, Email sent, Record created |
| **Warning** | Recoverable issues, validation failures | Invalid message, Duplicate message detected |
| **Error** | Failures requiring attention | SMTP errors, Database errors, Unexpected exceptions |
| **Debug** | Detailed troubleshooting info | Kafka partition/offset details (in KafkaConsumerUtility) |

### Logging by Layer

#### 1. KafkaBackgroundService (Entry Point)
**Location**: [NotificationService/Clients/WebApi/KafkaBackgroundService.cs](Clients/WebApi/KafkaBackgroundService.cs)

**Logs:**
- `LogInformation`: Service starting
- `LogInformation`: Topics subscribed
- `LogInformation`: Message received (includes Topic, MessageId)
- `LogInformation`: Processing success
- `LogWarning`: Processing failure (includes MessageId, Error)
- `LogError`: Exception during message processing
- `LogInformation`: Consumer operation cancelled
- `LogInformation`: Closing consumer
- `LogInformation`: Service stopping

**Key Fields:**
```csharp
_logger.LogInformation(
    "Received message from topic {Topic}: MessageId={MessageId}",
    message.Topic,
    message.MessageId);
```

#### 2. NotificationManager (Orchestration Layer)
**Location**: [NotificationService/Managers/NotificationManager.cs](Managers/NotificationManager.cs)

**Logs:**
- `LogWarning`: Invalid message validation (includes MessageId, Topic)
- `LogInformation`: Duplicate message detected (includes MessageId)
- `LogError`: Email send failure after retries (includes MessageId, Exception)
- `LogInformation`: Notification sent successfully (includes MessageId, Recipient)
- `LogError`: Unexpected error during processing (includes Exception)
- `LogError`: Retry operation failure (includes NotificationId, Exception)

**Key Fields:**
```csharp
_logger.LogInformation(
    "Notification sent successfully: MessageId={MessageId}, Recipient={Recipient}",
    message.MessageId,
    message.Recipient);

_logger.LogError(ex,
    "Failed to send email after retries: MessageId={MessageId}",
    message.MessageId);
```

#### 3. EmailAccessor (SMTP Layer)
**Location**: [NotificationService/Accessors/EmailAccessor.cs](Accessors/EmailAccessor.cs)

**Logs:**
- `LogInformation`: Email sent successfully (includes Recipient)
- `LogError`: SMTP error (includes Recipient, Exception)
- `LogError`: Unexpected error sending email (includes Recipient, Exception)

**Key Fields:**
```csharp
_logger.LogInformation("Email sent successfully to {Recipient}", request.To);
_logger.LogError(ex, "SMTP error sending email to {Recipient}", request.To);
```

#### 4. NotificationStorageAccessor (Database Layer)
**Location**: [NotificationService/Accessors/NotificationStorageAccessor.cs](Accessors/NotificationStorageAccessor.cs)

**Logs:**
- `LogInformation`: Record created (includes Id, MessageId)
- `LogError`: Database error on create (includes Exception)
- `LogInformation`: Record updated (includes Id)
- `LogError`: Database error on update (includes Exception)

**Key Fields:**
```csharp
_logger.LogInformation(
    "Created notification record with ID {Id} for message {MessageId}",
    record.Id,
    record.MessageId);
```

#### 5. KafkaConsumerUtility (Kafka Consumer)
**Location**: [NotificationService/Utilities/KafkaConsumerUtility.cs](Utilities/KafkaConsumerUtility.cs)

**Logs:**
- `LogInformation`: Topics subscribed
- `LogDebug`: Message consumed (includes Topic, Partition, Offset)
- `LogError`: Kafka consume exception
- `LogInformation`: Consumer operation cancelled
- `LogWarning`: Message parsing failure (includes Exception)
- `LogError`: Kafka offset commit error
- `LogInformation`: Consumer closed

**Key Fields:**
```csharp
_logger.LogDebug(
    "Consumed message from topic {Topic}, partition {Partition}, offset {Offset}",
    consumeResult.Topic,
    consumeResult.Partition.Value,
    consumeResult.Offset.Value);
```

## Distributed Tracing

### OpenTelemetry Configuration

**Location**: [NotificationService/Clients/WebApi/Program.cs](Clients/WebApi/Program.cs:14-32)

```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("NotificationService"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("NotificationService")
        .AddZipkinExporter(options =>
        {
            options.Endpoint = new Uri(builder.Configuration["Zipkin:Endpoint"]
                ?? "http://zipkin:9411/api/v2/spans");
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddPrometheusExporter());
```

### Activity/Span Creation

**TelemetryUtility**: [NotificationService/Utilities/TelemetryUtility.cs](Utilities/TelemetryUtility.cs)

**Activities Created:**
1. **ProcessAndSendNotification** - Full processing pipeline
   - Tags: `topic`, `message.id`, `message.recipient`
   - Duration: From message receive to email sent

2. **RetryFailedNotification** - Manual retry operation
   - Duration: Retry attempt execution

**Example:**
```csharp
using var activity = _telemetryUtility.StartActivity("ProcessAndSendNotification");
_telemetryUtility.SetTag("topic", message.Topic);
_telemetryUtility.SetTag("message.id", message.MessageId);
_telemetryUtility.SetTag("message.recipient", message.Recipient);
```

### Trace Propagation

OpenTelemetry automatically propagates trace context:
- **Inbound**: From Kafka messages (if producers add trace headers)
- **Outbound**: To SMTP connections (via HttpClientInstrumentation)
- **Between Services**: Via Dapr service invocation

## Metrics

### Prometheus Endpoint
**URL**: `http://localhost:8082/metrics`

### Custom Metrics Recorded

| Metric Name | Type | Description | Recorded In |
|-------------|------|-------------|-------------|
| `notification.sent` | Counter | Successfully sent notifications | NotificationManager:126 |
| `notification.failed` | Counter | Failed notification attempts | NotificationManager:107, 148 |
| `notification.error` | Counter | Unexpected errors | NotificationManager:161 |
| `notification.retry.success` | Counter | Successful retry attempts | NotificationManager:222 |
| `notification.retry.failed` | Counter | Failed retry attempts | NotificationManager:239 |

**Example:**
```csharp
_telemetryUtility.RecordMetric("notification.sent", 1);
_telemetryUtility.RecordMetric("notification.failed", 1);
```

### Built-in ASP.NET Core Metrics

Automatically collected by OpenTelemetry:
- HTTP request duration
- HTTP request count
- HTTP request errors
- HTTP client request duration
- HTTP client request count

### Viewing Metrics

**Prometheus UI**: http://localhost:9090
- Query: `notification_sent_total`
- Query: `notification_failed_total`
- Query: `http_server_request_duration_seconds`

**Grafana**: http://localhost:3000
- Pre-configured dashboards (if provisioned)
- Create custom dashboards for notification metrics

## Zipkin Distributed Tracing

### Viewing Traces

**Zipkin UI**: http://localhost:9411

**Search Criteria:**
- Service Name: `NotificationService`
- Span Name: `ProcessAndSendNotification`, `RetryFailedNotification`
- Tags: `topic`, `message.id`, `message.recipient`

**Trace Timeline Shows:**
1. Message consumption from Kafka
2. Manager orchestration
3. Email sending via SMTP
4. Database storage operations
5. Total end-to-end duration

### Example Trace
```
NotificationService: ProcessAndSendNotification [150ms]
  ├─ ValidateMessage [2ms]
  ├─ GetByMessageIdAsync (Database) [10ms]
  ├─ CreateAsync (Database) [15ms]
  ├─ BuildEmailRequest [1ms]
  ├─ SendEmailAsync (SMTP) [110ms]
  └─ UpdateAsync (Database) [12ms]
```

## Log Correlation

### Structured Logging Fields

All logs use structured logging with consistent fields:

**Common Fields:**
- `MessageId` - Unique message identifier (idempotency)
- `Topic` - Kafka topic name
- `Recipient` - Email recipient address
- `Id` - Database record identifier
- `NotificationId` - Retry operation identifier

### Correlation Example

**Search for all logs related to a specific message:**
```bash
docker-compose logs notification-api | grep "MessageId=alert-001"
```

**Output:**
```
[Information] Received message from topic weather-alerts: MessageId=alert-001
[Information] Created notification record with ID 12345 for message alert-001
[Information] Email sent successfully to user@example.com
[Information] Updated notification record with ID 12345
[Information] Notification sent successfully: MessageId=alert-001, Recipient=user@example.com
[Information] Successfully processed and sent notification for MessageId=alert-001
```

## Error Tracking

### Error Categories

#### 1. Validation Errors (WARNING)
- Invalid email format
- Missing required fields
- Duplicate messages (idempotency)

**Example Log:**
```
[Warning] Invalid message received: MessageId=bad-001, Topic=weather-alerts
```

#### 2. SMTP Errors (ERROR)
- Authentication failures
- Network timeouts
- Recipient rejected

**Example Log:**
```
[Error] SMTP error sending email to user@example.com
System.Net.Mail.SmtpException: Service not available
```

#### 3. Database Errors (ERROR)
- Connection failures
- Constraint violations
- Update conflicts

**Example Log:**
```
[Error] Database error creating notification record
Microsoft.EntityFrameworkCore.DbUpdateException: Unable to save changes
```

#### 4. Kafka Errors (ERROR)
- Consumer group authorization
- Topic not found
- Broker unavailable

**Example Log:**
```
[Error] Error consuming message from Kafka
Confluent.Kafka.ConsumeException: Group authorization failed
```

#### 5. Unexpected Errors (ERROR)
- Unhandled exceptions
- System errors

**Example Log:**
```
[Error] Unexpected error processing notification
System.Exception: Unexpected condition
```

## Monitoring Best Practices

### 1. Health Checks
**Endpoint**: `http://localhost:8082/health`

Monitor:
- SQLite database connectivity
- Service availability

### 2. Alert Rules (Prometheus)

**High Error Rate:**
```yaml
alert: HighNotificationFailureRate
expr: rate(notification_failed_total[5m]) > 0.1
for: 5m
severity: warning
```

**Service Down:**
```yaml
alert: NotificationServiceDown
expr: up{job="notification-api"} == 0
for: 1m
severity: critical
```

### 3. Key Metrics to Watch

| Metric | Threshold | Action |
|--------|-----------|--------|
| Error rate | > 5% | Investigate logs for SMTP/Kafka issues |
| Processing latency | > 5s | Check SMTP server performance |
| Kafka lag | > 1000 | Scale consumer or optimize processing |
| Retry rate | > 20% | Check email configuration |

### 4. Log Aggregation

**Recommended Setup:**
- Collect logs via Docker logging driver
- Send to centralized logging (ELK, Splunk, etc.)
- Create dashboards for error trends
- Set up alerts for error spikes

### 5. Trace Sampling

For production, configure trace sampling:
```csharp
.WithTracing(tracing => tracing
    .SetSampler(new TraceIdRatioBasedSampler(0.1)) // Sample 10% of traces
    // ... other configuration
)
```

## Troubleshooting Guide

### Issue: No traces in Zipkin
**Check:**
1. Zipkin endpoint configuration in appsettings.json
2. Network connectivity: `curl http://zipkin:9411`
3. OpenTelemetry configuration in Program.cs

### Issue: No metrics in Prometheus
**Check:**
1. Prometheus scrape configuration in prometheus.yml
2. Metrics endpoint: `curl http://localhost:8082/metrics`
3. Prometheus targets: http://localhost:9090/targets

### Issue: Missing logs
**Check:**
1. Log level configuration in appsettings.json
2. Docker logs: `docker-compose logs -f notification-api`
3. Log output configuration

### Issue: Logs not structured
**Check:**
- Using `ILogger<T>` from Microsoft.Extensions.Logging
- Using structured logging parameters: `_logger.LogInformation("Message {Field}", value)`
- Not using string interpolation in log messages

## Configuration

### appsettings.json - Logging

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning",
      "NotificationService": "Information"
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

### Environment Variables (docker-compose.yml)

```yaml
environment:
  - Zipkin__Endpoint=http://zipkin:9411/api/v2/spans
  - ASPNETCORE_ENVIRONMENT=Development
```

## Summary

The NotificationService has **comprehensive observability**:

✅ **Structured Logging** at every layer
✅ **Error Logging** with full exception details
✅ **Distributed Tracing** via OpenTelemetry + Zipkin
✅ **Custom Metrics** for business operations
✅ **Built-in Metrics** for HTTP/infrastructure
✅ **Health Checks** for service monitoring
✅ **Prometheus Integration** for metrics scraping
✅ **Correlation IDs** via MessageId throughout the pipeline
✅ **Retry Tracking** for failed notifications
✅ **Kafka Consumer Instrumentation** with partition/offset details

All errors are logged with full context and can be traced end-to-end through Zipkin.
