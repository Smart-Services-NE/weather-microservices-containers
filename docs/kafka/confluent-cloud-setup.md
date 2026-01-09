# Confluent Cloud Kafka Setup

Complete guide for connecting the NotificationService to Confluent Cloud Kafka.

## Prerequisites

- Confluent Cloud account with Kafka cluster
- API Key and Secret
- Topics created: `weather-alerts`, `general-events`

## Configuration Steps

### 1. Get Confluent Cloud Credentials

1. Log in to [Confluent Cloud](https://confluent.cloud/)
2. Select your Kafka cluster
3. Go to **Cluster Settings** → **API Keys**
4. Click **Add Key** and save the credentials

### 2. Get Bootstrap Servers

In Cluster Settings, copy the **Bootstrap server** endpoint:
```
pkc-xxxxx.us-east-1.aws.confluent.cloud:9092
```

### 3. Configure Environment Variables

Create/edit `.env` in the project root:

```bash
# Confluent Cloud Kafka Configuration
KAFKA_BOOTSTRAP_SERVERS=pkc-xxxxx.us-east-1.aws.confluent.cloud:9092
KAFKA_SECURITY_PROTOCOL=SASL_SSL
KAFKA_SASL_MECHANISM=PLAIN
KAFKA_SASL_USERNAME=YOUR_API_KEY
KAFKA_SASL_PASSWORD=YOUR_API_SECRET
```

### 4. Update Docker Compose

Update `docker-compose.yml` for notification-api:

```yaml
notification-api:
  environment:
    - Kafka__BootstrapServers=${KAFKA_BOOTSTRAP_SERVERS}
    - Kafka__SecurityProtocol=${KAFKA_SECURITY_PROTOCOL}
    - Kafka__SaslMechanism=${KAFKA_SASL_MECHANISM}
    - Kafka__SaslUsername=${KAFKA_SASL_USERNAME}
    - Kafka__SaslPassword=${KAFKA_SASL_PASSWORD}
  depends_on:
    zipkin:
      condition: service_started
    # Remove local kafka dependency
```

Comment out local Kafka/Zookeeper services if using Confluent Cloud exclusively.

### 5. Create Topics

In Confluent Cloud Console:
1. Go to **Topics** → **Add topic**
2. Create topics:
   - `weather-alerts`
   - `general-events`
3. Use default settings (1 partition, 7-day retention)

### 6. Start the Service

```bash
docker-compose build notification-api
docker-compose up -d notification-api
docker-compose logs -f notification-api
```

You should see: `Subscribed to Kafka topics: weather-alerts, general-events`

## Testing

### Via Confluent Cloud Console

1. Go to Topics → `weather-alerts` → **Produce a new message**
2. Set **Value format** to **String** (for JSON) or **Avro** (if using Schema Registry)
3. Paste test message:

```json
{
  "messageId": "test-001",
  "subject": "Test Notification",
  "body": "Testing Confluent Cloud connection",
  "recipient": "user@example.com",
  "timestamp": 1735574400000,
  "metadata": {
    "from": "test@weatherapp.com",
    "isHtml": "false"
  }
}
```

### Verify Processing

```bash
docker-compose logs notification-api | grep "Notification sent successfully"
```

## Troubleshooting

### Connection Errors

**Error**: `Failed to resolve 'pkc-xxxxx.confluent.cloud'`
- Check network connection and DNS settings

**Error**: `Authentication failed`
- Verify API Key and Secret are correct

**Error**: `Subscribed topic not available`
- Create topics in Confluent Cloud first

### Verify Configuration

```bash
# Check loaded environment variables
docker-compose exec notification-api env | grep KAFKA

# Enable debug logging in appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Confluent.Kafka": "Debug"
    }
  }
}
```

## Local vs Confluent Cloud

| Aspect | Local Kafka | Confluent Cloud |
|--------|-------------|-----------------|
| Bootstrap Servers | `kafka:9093` or `localhost:9092` | `pkc-xxxxx...confluent.cloud:9092` |
| Security Protocol | `Plaintext` | `SASL_SSL` |
| Authentication | None | API Key/Secret |
| Dependencies | Zookeeper + Kafka containers | None |

## Security Best Practices

- Store credentials in `.env` file (already in `.gitignore`)
- Use API keys with minimal required permissions
- Rotate API keys periodically
- Use separate keys for dev/staging/prod

## Related Documentation

- [Avro Schema Registry Setup](./avro-setup.md)
- [Kafka Troubleshooting](./troubleshooting.md)
- [Message Schemas](../infrastructure/schemas.md)
