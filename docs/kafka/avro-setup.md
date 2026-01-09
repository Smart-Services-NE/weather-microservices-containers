# Avro Schema Registry Setup

Configure the NotificationService to consume Avro-serialized messages using Confluent Schema Registry.

## Overview

The service automatically detects Schema Registry configuration and switches between JSON and Avro deserialization modes.

## Prerequisites

- Confluent Cloud Kafka configured ([see setup guide](./confluent-cloud-setup.md))
- Schema Registry enabled in Confluent Cloud
- Avro schemas registered for your topics

## Quick Setup

### 1. Get Schema Registry Credentials

1. Log in to [Confluent Cloud](https://confluent.cloud)
2. Navigate to **Environments** → **Schema Registry**
3. Copy the **URL** (e.g., `https://psrc-xxxxx.us-east-2.aws.confluent.cloud`)
4. Click **API credentials** → **+ Add key**
5. Save the API Key and Secret

### 2. Update Environment Variables

Edit `.env` file:

```bash
# Confluent Cloud Schema Registry Configuration
KAFKA_SCHEMA_REGISTRY_URL=https://psrc-xxxxx.us-east-2.aws.confluent.cloud
KAFKA_SCHEMA_REGISTRY_KEY=YOUR_SR_API_KEY
KAFKA_SCHEMA_REGISTRY_SECRET=YOUR_SR_API_SECRET
```

### 3. Update Docker Compose

Add to `docker-compose.yml` under notification-api:

```yaml
notification-api:
  environment:
    # ... existing Kafka config ...
    - Kafka__SchemaRegistryUrl=${KAFKA_SCHEMA_REGISTRY_URL}
    - Kafka__SchemaRegistryKey=${KAFKA_SCHEMA_REGISTRY_KEY}
    - Kafka__SchemaRegistrySecret=${KAFKA_SCHEMA_REGISTRY_SECRET}
```

### 4. Rebuild and Restart

```bash
docker-compose build --no-cache notification-api
docker-compose up -d notification-api
docker-compose logs -f notification-api | head -30
```

### 5. Verify Success

Look for these log messages:

```
[Information] Schema Registry configured with authentication
[Information] Avro Kafka Consumer initialized with Schema Registry: https://psrc-xxxxx...
[Information] Successfully deserialized Avro message: MessageId=xxx, Subject=xxx
```

## How It Works

### Automatic Mode Detection

The service automatically chooses the correct consumer:

```csharp
var schemaRegistryUrl = configuration["Kafka:SchemaRegistryUrl"];
if (!string.IsNullOrEmpty(schemaRegistryUrl))
{
    // Use Avro consumer with Schema Registry
    services.AddSingleton<IKafkaConsumerUtility, AvroKafkaConsumerUtility>();
}
else
{
    // Use JSON consumer (backwards compatible)
    services.AddSingleton<IKafkaConsumerUtility, KafkaConsumerUtility>();
}
```

### Message Flow

```
Confluent Cloud Topic (Avro binary + Schema ID)
           ↓
AvroKafkaConsumerUtility
           ↓
Schema Registry Client (fetches schema by ID)
           ↓
Avro Deserializer<AvroNotificationMessage>
           ↓
AvroNotificationMessage.ToNotificationMessage()
           ↓
NotificationMessage (domain model)
           ↓
NotificationManager (existing logic - unchanged)
```

## Schema Compatibility

The `AvroNotificationMessage` class matches the `notification-message.avsc` schema:

```json
{
  "type": "record",
  "name": "NotificationMessage",
  "namespace": "com.weatherapp.notifications",
  "fields": [
    { "name": "messageId", "type": "string" },
    { "name": "subject", "type": "string" },
    { "name": "body", "type": "string" },
    { "name": "recipient", "type": "string" },
    { "name": "timestamp", "type": { "type": "long", "logicalType": "timestamp-millis" } },
    { "name": "metadata", "type": ["null", { "type": "map", "values": "string" }] }
  ]
}
```

## Testing

### Send Avro Message via Confluent Cloud Console

1. Go to Topics → `weather-alerts` → **Produce a new message**
2. Set **Value format** to **Avro**
3. Select schema: `notification-message` (or `weather-alert` for rich alerts)
4. Fill in form fields or paste JSON
5. Click **Produce**

### Verify Processing

```bash
docker-compose logs -f notification-api | grep "Successfully deserialized Avro"
```

## Troubleshooting

### Schema Registry Not Found

**Error**: `Schema Registry URL is required for Avro deserialization`

**Solution**: Check `.env` file has correct URL

### Authentication Failed

**Error**: `401 Unauthorized`

**Solution**: Verify API Key and Secret are correct

### Schema Not Found

**Error**: `Schema not found`

**Solution**: Register the schema in Confluent Cloud:

```bash
# Verify schema is registered
curl -u "$KAFKA_SCHEMA_REGISTRY_KEY:$KAFKA_SCHEMA_REGISTRY_SECRET" \
  "$KAFKA_SCHEMA_REGISTRY_URL/subjects/weather-alerts-value/versions"
```

### Still Seeing JSON Parse Errors

**Issue**: Messages appear to be Avro but service uses JSON parser

**Solution**: Ensure Schema Registry environment variables are loaded:

```bash
docker-compose exec notification-api env | grep SCHEMA_REGISTRY
```

## Benefits

- **Type Safety**: Compile-time type checking
- **Schema Evolution**: Backward/forward compatibility
- **Auto-validation**: Messages validated against schema
- **Smaller Messages**: Binary format is more compact
- **Schema Caching**: Automatic schema caching for performance

## Related Documentation

- [Message Schemas](../infrastructure/schemas.md)
- [Confluent Cloud Setup](./confluent-cloud-setup.md)
- [Kafka Troubleshooting](./troubleshooting.md)
