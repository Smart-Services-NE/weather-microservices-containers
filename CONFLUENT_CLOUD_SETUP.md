# Confluent Cloud Configuration Guide

This guide explains how to configure the NotificationService to connect to Confluent Cloud Kafka.

## Prerequisites

1. A Confluent Cloud account with a Kafka cluster
2. API Key and Secret created in Confluent Cloud
3. Topics created in your Confluent Cloud cluster (`weather-alerts`, `general-events`)

## Configuration Steps

### Step 1: Get Confluent Cloud Credentials

1. Log in to [Confluent Cloud](https://confluent.cloud/)
2. Select your Kafka cluster
3. Go to **Cluster Settings** → **API Keys**
4. Click **Add Key** to create a new API Key/Secret pair
5. Save the API Key and Secret (you won't be able to see the secret again)

### Step 2: Get Bootstrap Servers

1. In your Confluent Cloud cluster, go to **Cluster Settings**
2. Copy the **Bootstrap server** endpoint (e.g., `pkc-xxxxx.us-east-1.aws.confluent.cloud:9092`)

### Step 3: Configure for Docker Deployment

#### Option A: Using Environment Variables (Recommended)

Create a `.env` file in the project root:

```bash
cp .env.example .env
```

Edit `.env` with your Confluent Cloud credentials:

```bash
# Confluent Cloud Kafka Configuration
KAFKA_BOOTSTRAP_SERVERS=pkc-xxxxx.us-east-1.aws.confluent.cloud:9092
KAFKA_SECURITY_PROTOCOL=SASL_SSL
KAFKA_SASL_MECHANISM=PLAIN
KAFKA_SASL_USERNAME=YOUR_API_KEY
KAFKA_SASL_PASSWORD=YOUR_API_SECRET
```

Update `docker-compose.yml` to use Confluent Cloud instead of local Kafka:

```yaml
notification-api:
  environment:
    - Kafka__BootstrapServers=${KAFKA_BOOTSTRAP_SERVERS}
    - Kafka__SecurityProtocol=${KAFKA_SECURITY_PROTOCOL}
    - Kafka__SaslMechanism=${KAFKA_SASL_MECHANISM}
    - Kafka__SaslUsername=${KAFKA_SASL_USERNAME}
    - Kafka__SaslPassword=${KAFKA_SASL_PASSWORD}
  depends_on:
    # Remove kafka dependency since using Confluent Cloud
    zipkin:
      condition: service_started
```

#### Option B: Using appsettings.json (Not Recommended for Production)

Edit `NotificationService/Clients/WebApi/appsettings.json`:

```json
{
  "Kafka": {
    "BootstrapServers": "pkc-xxxxx.us-east-1.aws.confluent.cloud:9092",
    "GroupId": "notification-service",
    "Topics": ["weather-alerts", "general-events"],
    "SecurityProtocol": "SASL_SSL",
    "SaslMechanism": "PLAIN",
    "SaslUsername": "YOUR_API_KEY",
    "SaslPassword": "YOUR_API_SECRET"
  }
}
```

**⚠️ Warning:** Never commit credentials to source control. Use environment variables or secret management instead.

### Step 4: Create Topics in Confluent Cloud

1. In Confluent Cloud, go to **Topics**
2. Click **Add topic**
3. Create the following topics:
   - `weather-alerts`
   - `general-events`
4. Use default settings (partitions: 1, retention: 7 days)

### Step 5: Update Docker Compose for Confluent Cloud

Since you're using Confluent Cloud, you no longer need the local Kafka and Zookeeper containers. Update your `docker-compose.yml`:

**Remove these services** (or comment them out):
```yaml
# zookeeper:
#   ...

# kafka:
#   ...
```

**Update notification-api dependencies**:
```yaml
notification-api:
  depends_on:
    zipkin:
      condition: service_started
  # Remove kafka dependency
```

### Step 6: Start the NotificationService

```bash
# Build and start the service
docker-compose build notification-api
docker-compose up -d notification-api

# Check logs
docker-compose logs -f notification-api
```

You should see:
```
Subscribed to Kafka topics: weather-alerts, general-events
```

## Testing the Connection

### Send a Test Message via Confluent Cloud CLI

If you have the Confluent CLI installed:

```bash
confluent kafka topic produce weather-alerts \
  --cluster <cluster-id> \
  --api-key <api-key> \
  --api-secret <api-secret>
```

Then paste this JSON:
```json
{"messageId":"confluent-test-1","subject":"Confluent Cloud Test","body":"Testing connection from Confluent Cloud","recipient":"test@example.com","timestamp":1735574400000,"metadata":{"from":"confluent@weatherapp.com"}}
```

### Send a Test Message via Confluent Cloud Console

1. Go to your Confluent Cloud cluster
2. Navigate to **Topics** → `weather-alerts`
3. Click **Produce a new message to this topic**
4. Use the JSON format above

### Verify Message Processing

Check the NotificationService logs:
```bash
docker-compose logs notification-api | grep "Received message"
```

You should see:
```
Received message from topic weather-alerts: MessageId=confluent-test-1
```

## Configuration Reference

### Kafka Settings

| Setting | Description | Confluent Cloud Value |
|---------|-------------|----------------------|
| `BootstrapServers` | Kafka cluster endpoint | `pkc-xxxxx.region.provider.confluent.cloud:9092` |
| `SecurityProtocol` | Security protocol | `SASL_SSL` |
| `SaslMechanism` | Authentication mechanism | `PLAIN` |
| `SaslUsername` | API Key | From Confluent Cloud |
| `SaslPassword` | API Secret | From Confluent Cloud |
| `GroupId` | Consumer group ID | `notification-service` (or custom) |
| `Topics` | Topics to subscribe to | `["weather-alerts", "general-events"]` |

### Local vs Confluent Cloud Configuration

| Aspect | Local Kafka | Confluent Cloud |
|--------|-------------|-----------------|
| Bootstrap Servers | `kafka:9093` (Docker) or `localhost:9092` (host) | `pkc-xxxxx.region.provider.confluent.cloud:9092` |
| Security Protocol | `Plaintext` | `SASL_SSL` |
| Authentication | None | API Key/Secret |
| Dependencies | Requires Zookeeper + Kafka containers | No local containers needed |

## Troubleshooting

### Connection Errors

**Error:** `Failed to resolve 'pkc-xxxxx.confluent.cloud'`
- **Solution:** Check your network connection and DNS settings

**Error:** `Authentication failed`
- **Solution:** Verify your API Key and Secret are correct

**Error:** `Subscribed topic not available`
- **Solution:** Create the topics in Confluent Cloud first

### Verify Configuration

Check what configuration the service is using:
```bash
docker-compose exec notification-api env | grep KAFKA
```

### Enable Debug Logging

Update `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Confluent.Kafka": "Debug"
    }
  }
}
```

## Security Best Practices

1. **Never commit credentials** - Use environment variables or secret managers
2. **Rotate API Keys** - Regularly rotate your Confluent Cloud API keys
3. **Use separate keys** - Use different API keys for dev/staging/production
4. **Restrict permissions** - Create API keys with minimal required permissions
5. **Use .gitignore** - Ensure `.env` is in `.gitignore`

## Schema Registry (Optional)

If you want to use the Avro schemas in `schemas/avro/`, configure Confluent Cloud Schema Registry:

```bash
# Add to .env
SCHEMA_REGISTRY_URL=https://psrc-xxxxx.region.provider.confluent.cloud
SCHEMA_REGISTRY_API_KEY=SR_API_KEY
SCHEMA_REGISTRY_API_SECRET=SR_API_SECRET
```

See [schemas/README.md](schemas/README.md) for more details on using Avro schemas with Confluent Cloud.

## Additional Resources

- [Confluent Cloud Documentation](https://docs.confluent.io/cloud/current/overview.html)
- [Confluent Kafka .NET Client](https://docs.confluent.io/kafka-clients/dotnet/current/overview.html)
- [Confluent Cloud Quickstart](https://docs.confluent.io/cloud/current/get-started/index.html)
