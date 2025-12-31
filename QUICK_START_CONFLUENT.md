# Quick Start: Confluent Cloud Setup

## TL;DR - 3 Steps to Connect

### 1. Create `.env` file in project root

```bash
# Copy template
cp .env.example .env

# Edit with your Confluent Cloud credentials
nano .env
```

Add your Confluent Cloud details:
```bash
KAFKA_BOOTSTRAP_SERVERS=pkc-xxxxx.us-east-1.aws.confluent.cloud:9092
KAFKA_SECURITY_PROTOCOL=SASL_SSL
KAFKA_SASL_MECHANISM=PLAIN
KAFKA_SASL_USERNAME=YOUR_API_KEY
KAFKA_SASL_PASSWORD=YOUR_API_SECRET
```

### 2. Update `docker-compose.yml`

Replace the `notification-api` environment section with:

```yaml
notification-api:
  environment:
    - ASPNETCORE_HTTP_PORTS=8080
    - ASPNETCORE_ENVIRONMENT=Development
    # Use Confluent Cloud instead of local Kafka
    - Kafka__BootstrapServers=${KAFKA_BOOTSTRAP_SERVERS}
    - Kafka__GroupId=notification-service
    - Kafka__SecurityProtocol=${KAFKA_SECURITY_PROTOCOL}
    - Kafka__SaslMechanism=${KAFKA_SASL_MECHANISM}
    - Kafka__SaslUsername=${KAFKA_SASL_USERNAME}
    - Kafka__SaslPassword=${KAFKA_SASL_PASSWORD}
    - Email__SmtpHost=smtp.gmail.com
    - Email__SmtpPort=587
    - Email__EnableSsl=true
    - ConnectionStrings__NotificationDb=Data Source=/app/data/notifications.db
    - Zipkin__Endpoint=http://zipkin:9411/api/v2/spans
  depends_on:
    # Remove kafka dependency, only need zipkin
    zipkin:
      condition: service_started
```

**Comment out or remove local Kafka/Zookeeper** (lines 73-100 in docker-compose.yml):
```yaml
# zookeeper:
#   ...
# kafka:
#   ...
```

### 3. Start the service

```bash
# Build and start
docker-compose build notification-api
docker-compose up -d notification-api zipkin

# Check logs - should see "Subscribed to Kafka topics"
docker-compose logs -f notification-api
```

## Test Message Format

Send test messages via Confluent Cloud Console or CLI:

```json
{
  "messageId": "test-123",
  "subject": "Test Alert",
  "body": "Testing Confluent Cloud connection",
  "recipient": "user@example.com",
  "timestamp": 1735574400000,
  "metadata": {
    "from": "test@weatherapp.com",
    "isHtml": "false"
  }
}
```

## Where to Get Confluent Cloud Credentials

1. **Bootstrap Servers**: Cluster Settings → Cluster → Bootstrap server
2. **API Key/Secret**: Cluster Settings → API Keys → Add key

## Verify Connection

```bash
# Check if connected
docker-compose logs notification-api | grep "Subscribed to Kafka"

# Should output:
# Subscribed to Kafka topics: weather-alerts, general-events
```

## Full Documentation

See [CONFLUENT_CLOUD_SETUP.md](CONFLUENT_CLOUD_SETUP.md) for complete configuration details.
