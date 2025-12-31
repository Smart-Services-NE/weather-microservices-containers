# Kafka Message Schemas

This directory contains Avro schemas for Kafka topics used by the NotificationService.

## Available Schemas

### 1. notification-message.avsc
**Topic**: `weather-alerts`, `general-events`
**Description**: Generic notification message schema compatible with NotificationService

This is the base schema that the NotificationService expects. Use this for simple email notifications.

**Fields**:
- `messageId` (string, required): Unique identifier for idempotency
- `subject` (string, required): Email subject
- `body` (string, required): Email body (plain text or HTML)
- `recipient` (string, required): Valid email address
- `timestamp` (long, required): Unix timestamp in milliseconds
- `metadata` (map<string>, optional): Additional metadata
  - `from`: Custom sender email
  - `isHtml`: "true" or "false" for HTML body

### 2. weather-alert.avsc
**Topic**: `weather-alerts`
**Description**: Rich weather alert schema with structured data

This schema includes detailed weather information and is useful for weather-specific alerts.

**Additional Fields** (beyond NotificationMessage):
- `alertType` (enum): Type of weather alert
- `severity` (enum): INFO, WARNING, SEVERE, CRITICAL
- `location` (record): Geographic location with zipCode, city, state, coordinates
- `weatherConditions` (record): Current conditions (temperature, weatherCode, description, wind, precipitation)

## Usage Examples

### Example 1: Simple Notification (JSON)

```json
{
  "messageId": "550e8400-e29b-41d4-a716-446655440000",
  "subject": "Test Notification",
  "body": "<h1>Hello!</h1><p>This is a test notification.</p>",
  "recipient": "user@example.com",
  "timestamp": 1735574400000,
  "metadata": {
    "from": "alerts@weatherapp.com",
    "isHtml": "true"
  }
}
```

### Example 2: Weather Alert (JSON)

```json
{
  "messageId": "660e8400-e29b-41d4-a716-446655440001",
  "subject": "Severe Weather Alert for 94105",
  "body": "<h2>Severe Weather Warning</h2><p>Heavy rain and strong winds expected in San Francisco, CA.</p><p>Current temperature: 15.5°C</p>",
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

### Example 3: General Event (JSON)

```json
{
  "messageId": "770e8400-e29b-41d4-a716-446655440002",
  "subject": "Welcome to Weather App!",
  "body": "Thank you for signing up. You will now receive weather alerts for your location.",
  "recipient": "newuser@example.com",
  "timestamp": 1735574400000,
  "metadata": {
    "from": "noreply@weatherapp.com",
    "isHtml": "false"
  }
}
```

## Publishing Messages to Kafka

### Using kafka-console-producer (JSON)

```bash
# Connect to Kafka
docker exec -it containerapp-kafka-1 bash

# Publish to weather-alerts topic
kafka-console-producer --broker-list localhost:9093 --topic weather-alerts

# Then paste your JSON message (one line, no newlines in the JSON)
{"messageId":"test-001","subject":"Test Alert","body":"Test message","recipient":"test@example.com","timestamp":1735574400000}
```

### Using Confluent Schema Registry (Avro)

```bash
# Register the schema
curl -X POST -H "Content-Type: application/vnd.schemaregistry.v1+json" \
  --data @notification-message.avsc \
  http://localhost:8081/subjects/weather-alerts-value/versions

# Publish Avro-encoded message using confluent-kafka-python
# (See producer example below)
```

### Python Producer Example (with Avro)

```python
from confluent_kafka import avro
from confluent_kafka.avro import AvroProducer
import time

# Avro schema
value_schema_str = """
{
  "type": "record",
  "name": "NotificationMessage",
  "fields": [
    {"name": "messageId", "type": "string"},
    {"name": "subject", "type": "string"},
    {"name": "body", "type": "string"},
    {"name": "recipient", "type": "string"},
    {"name": "timestamp", "type": {"type": "long", "logicalType": "timestamp-millis"}},
    {"name": "metadata", "type": ["null", {"type": "map", "values": "string"}], "default": null}
  ]
}
"""

value_schema = avro.loads(value_schema_str)

avroProducer = AvroProducer({
    'bootstrap.servers': 'localhost:9092',
    'schema.registry.url': 'http://localhost:8081'
}, default_value_schema=value_schema)

# Create message
value = {
    "messageId": "test-001",
    "subject": "Weather Alert",
    "body": "<h1>Severe Weather</h1>",
    "recipient": "user@example.com",
    "timestamp": int(time.time() * 1000),
    "metadata": {"from": "alerts@weatherapp.com", "isHtml": "true"}
}

# Produce message
avroProducer.produce(topic='weather-alerts', value=value)
avroProducer.flush()
```

### C# Producer Example (with JSON)

```csharp
using Confluent.Kafka;
using System.Text.Json;

var config = new ProducerConfig { BootstrapServers = "localhost:9092" };

using var producer = new ProducerBuilder<Null, string>(config).Build();

var message = new
{
    messageId = Guid.NewGuid().ToString(),
    subject = "Weather Alert",
    body = "<h1>Severe Weather Warning</h1>",
    recipient = "user@example.com",
    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
    metadata = new Dictionary<string, string>
    {
        ["from"] = "alerts@weatherapp.com",
        ["isHtml"] = "true"
    }
};

var json = JsonSerializer.Serialize(message);

await producer.ProduceAsync("weather-alerts", new Message<Null, string>
{
    Value = json
});
```

## Schema Validation

The NotificationService will validate messages against these requirements:

1. **Required Fields**: messageId, subject, body, recipient must be present
2. **Email Validation**: recipient must be a valid email format
3. **Idempotency**: Duplicate messageId values are detected and skipped
4. **Timestamp**: If missing, defaults to current UTC time

Invalid messages are logged but do not cause the service to crash. The Kafka offset is committed to avoid reprocessing.

## NotificationService Processing Flow

```
Kafka Topic (weather-alerts/general-events)
    ↓
KafkaConsumerUtility.ConsumeMessageAsync()
    ↓ (Parses JSON to NotificationMessage)
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

When using the `weather-alert.avsc` schema, the `weatherCode` field follows the Open-Meteo WMO Weather interpretation codes:

- 0: Clear sky
- 1-3: Mainly clear, partly cloudy, overcast
- 45-48: Fog
- 51-57: Drizzle
- 61-67: Rain
- 71-77: Snow
- 80-82: Rain showers
- 85-86: Snow showers
- 95-99: Thunderstorm

See [NotificationService/Engines/NotificationEngine.cs](../NotificationService/Engines/NotificationEngine.cs) for parsing logic.

## Schema Registry Setup (Optional)

If you want to use Confluent Schema Registry for schema validation:

```yaml
# Add to docker-compose.yml
schema-registry:
  image: confluentinc/cp-schema-registry:latest
  depends_on:
    - kafka
  ports:
    - "8081:8081"
  environment:
    SCHEMA_REGISTRY_HOST_NAME: schema-registry
    SCHEMA_REGISTRY_KAFKASTORE_BOOTSTRAP_SERVERS: kafka:9093
```

Then register schemas:

```bash
# Register notification-message schema
curl -X POST -H "Content-Type: application/vnd.schemaregistry.v1+json" \
  --data @notification-message.avsc \
  http://localhost:8081/subjects/weather-alerts-value/versions

# Register weather-alert schema
curl -X POST -H "Content-Type: application/vnd.schemaregistry.v1+json" \
  --data @weather-alert.avsc \
  http://localhost:8081/subjects/weather-alerts-value/versions
```

## Testing

To test the NotificationService with sample messages:

```bash
# 1. Start all services
docker-compose up -d

# 2. Check NotificationService is running
curl http://localhost:8082/health

# 3. Publish a test message
docker exec -it containerapp-kafka-1 kafka-console-producer \
  --broker-list localhost:9093 \
  --topic weather-alerts

# 4. Paste this message:
{"messageId":"test-001","subject":"Test Weather Alert","body":"This is a test alert","recipient":"test@example.com","timestamp":1735574400000,"metadata":{"isHtml":"false"}}

# 5. Check NotificationService logs
docker-compose logs -f notification-api

# 6. Verify in database
docker exec -it containerapp-notification-api-1 sqlite3 /app/data/notifications.db \
  "SELECT * FROM Notifications WHERE MessageId='test-001';"
```

## Troubleshooting

**Message not being consumed:**
- Check Kafka is running: `docker-compose ps kafka`
- Verify topic exists: `kafka-topics --list --bootstrap-server localhost:9092`
- Check consumer group: `kafka-consumer-groups --bootstrap-server localhost:9092 --group notification-service --describe`

**Email not being sent:**
- Check SMTP configuration in `appsettings.json` or environment variables
- Verify email validation passed (check logs)
- Check retry logic hasn't exhausted (max 5 retries)

**Duplicate messages:**
- The service uses `messageId` for idempotency - duplicates are automatically skipped
- Check logs for "Duplicate message detected" warnings
