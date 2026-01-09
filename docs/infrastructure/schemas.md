# Kafka Message Schemas

Avro schemas for Kafka topics used by the NotificationService.

## Available Schemas

### 1. notification-message.avsc

**Topics**: `weather-alerts`, `general-events`
**Location**: `schemas/avro/notification-message.avsc`
**Description**: Generic notification message compatible with NotificationService

**Use for**: Simple email notifications

**Fields**:
- `messageId` (string, required) - Unique identifier for idempotency
- `subject` (string, required) - Email subject
- `body` (string, required) - Email body (plain text or HTML)
- `recipient` (string, required) - Valid email address
- `timestamp` (long, required) - Unix timestamp in milliseconds
- `metadata` (map<string>, optional) - Additional metadata
  - `from` - Custom sender email
  - `isHtml` - "true" or "false" for HTML body

**Example**:
```json
{
  "messageId": "test-001",
  "subject": "Test Notification",
  "body": "<h1>Hello!</h1><p>This is a test.</p>",
  "recipient": "user@example.com",
  "timestamp": 1735574400000,
  "metadata": {
    "from": "alerts@weatherapp.com",
    "isHtml": "true"
  }
}
```

### 2. weather-alert.avsc

**Topic**: `weather-alerts`
**Location**: `schemas/avro/weather-alert.avsc`
**Description**: Rich weather alert schema with structured data

**Use for**: Weather-specific alerts with detailed conditions

**Additional Fields** (beyond NotificationMessage):
- `alertType` (enum) - Type of weather alert
- `severity` (enum) - INFO, WARNING, SEVERE, CRITICAL
- `location` (record) - Geographic location with zipCode, city, state, coordinates
- `weatherConditions` (record) - Current conditions (temperature, weatherCode, wind, precipitation)

**Example**:
```json
{
  "messageId": "alert-001",
  "subject": "Severe Weather Alert for 94105",
  "body": "<h2>Severe Weather Warning</h2><p>Heavy rain and strong winds expected.</p>",
  "recipient": "user@example.com",
  "timestamp": 1735574400000,
  "alertType": "SEVERE_WEATHER",
  "severity": "SEVERE",
  "location": {
    "zipCode": "94105",
    "city": "San Francisco",
    "state": "California",
    "latitude": 37.7749,
    "longitude": -122.4194
  },
  "weatherConditions": {
    "currentTemperature": 15.5,
    "weatherCode": 95,
    "weatherDescription": "Thunderstorm with heavy hail",
    "windSpeed": 45.0,
    "precipitation": 25.5
  },
  "metadata": {
    "from": "weather-alerts@weatherapp.com",
    "isHtml": "true",
    "priority": "high"
  }
}
```

## Message Formats

### JSON (String Format)

Send as plain JSON string without Schema Registry:

```bash
# Via Confluent Cloud Console:
# 1. Set "Value format" to "String"
# 2. Paste JSON payload
```

### Avro (Binary Format)

Send using Schema Registry:

```bash
# Via Confluent Cloud Console:
# 1. Set "Value format" to "Avro"
# 2. Select schema version
# 3. Fill form or paste JSON
```

## Schema Validation

The NotificationService validates messages:

1. **Required Fields**: messageId, subject, body, recipient
2. **Email Validation**: recipient must be valid email format
3. **Idempotency**: Duplicate messageId values are skipped
4. **Timestamp**: Defaults to current UTC time if missing

Invalid messages are logged but don't crash the service.

## NotificationService Processing Flow

```
Kafka Topic (weather-alerts/general-events)
    ↓
KafkaConsumerUtility.ConsumeMessageAsync()
    ↓ (Parse JSON or deserialize Avro)
NotificationManager.ProcessAndSendNotificationAsync()
    ↓
NotificationEngine.ValidateMessage()
    ↓ (Check for duplicate messageId)
NotificationStorageAccessor.GetByMessageIdAsync()
    ↓ (Build email request)
NotificationEngine.BuildEmailRequest()
    ↓ (Send with retry policy)
EmailAccessor.SendEmailAsync()
    ↓
NotificationStorageAccessor.CreateAsync()
    ↓
KafkaConsumerUtility.CommitOffsetAsync()
```

## Weather Code Reference

For `weather-alert.avsc`, the `weatherCode` follows Open-Meteo WMO codes:

| Code | Description |
|------|-------------|
| 0 | Clear sky |
| 1-3 | Mainly clear, partly cloudy, overcast |
| 45-48 | Fog |
| 51-57 | Drizzle |
| 61-67 | Rain |
| 71-77 | Snow |
| 80-82 | Rain showers |
| 85-86 | Snow showers |
| 95-99 | Thunderstorm |

## Publishing Messages

### Via kafka-console-producer (JSON)

```bash
docker exec -i containerapp-kafka-1 kafka-console-producer \
  --broker-list localhost:9093 \
  --topic weather-alerts <<'EOF'
{"messageId":"test-001","subject":"Test","body":"Test","recipient":"user@example.com","timestamp":1735574400000}
EOF
```

### Via Python (JSON)

```python
from confluent_kafka import Producer
import json

producer = Producer({'bootstrap.servers': 'localhost:9092'})

message = {
    "messageId": "test-001",
    "subject": "Test",
    "body": "Test message",
    "recipient": "user@example.com",
    "timestamp": 1735574400000
}

producer.produce('weather-alerts', value=json.dumps(message))
producer.flush()
```

### Via Python (Avro)

See [Weather Alert Producer Guide](../scripts/weather-alert-producer.md)

## Testing

```bash
# Start services
docker-compose up -d

# Health check
curl http://localhost:8082/health

# Publish test message (JSON)
docker exec -i containerapp-kafka-1 kafka-console-producer \
  --broker-list localhost:9093 \
  --topic weather-alerts <<'EOF'
{"messageId":"test-001","subject":"Test Weather Alert","body":"Testing","recipient":"test@example.com","timestamp":1735574400000}
EOF

# Check logs
docker-compose logs -f notification-api | grep "test-001"

# Verify in database
docker exec -it containerapp-notification-api-1 sqlite3 /app/data/notifications.db \
  "SELECT * FROM Notifications WHERE MessageId='test-001';"
```

## Schema Registry Setup

See [Avro Setup Guide](../kafka/avro-setup.md) for complete instructions.

**Quick setup**:

```bash
# Add to .env
KAFKA_SCHEMA_REGISTRY_URL=https://psrc-xxxxx.confluent.cloud
KAFKA_SCHEMA_REGISTRY_KEY=YOUR_KEY
KAFKA_SCHEMA_REGISTRY_SECRET=YOUR_SECRET

# Rebuild service
docker-compose build --no-cache notification-api
docker-compose up -d notification-api
```

## Troubleshooting

**Message not being consumed**:
- Check Kafka is running: `docker-compose ps kafka`
- Verify topic exists: `kafka-topics --list --bootstrap-server localhost:9092`
- Check consumer group: `kafka-consumer-groups --describe --group notification-service`

**Email not sent**:
- Check SMTP configuration in `appsettings.json`
- Verify email validation passed (check logs)
- Check retry logic hasn't exhausted (max 5 retries)

**Duplicate messages**:
- Service uses `messageId` for idempotency
- Duplicates automatically skipped
- Check logs for "Duplicate message detected"

## Related Documentation

- [Avro Setup Guide](../kafka/avro-setup.md)
- [Confluent Cloud Setup](../kafka/confluent-cloud-setup.md)
- [Weather Alert Producer](../scripts/weather-alert-producer.md)
- [Kafka Troubleshooting](../kafka/troubleshooting.md)
