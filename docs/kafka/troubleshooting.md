# Kafka Troubleshooting Guide

Common issues and solutions for Kafka message processing.

## Message Format Issues

### Problem: "0x00 is an invalid start of a value"

**Symptoms**:
```
System.Text.Json.JsonReaderException: '0x00' is an invalid start of a value
Failed to parse Kafka message
```

**Root Cause**: Service is configured for JSON but receives Avro binary format with Schema Registry encoding.

**Solutions**:

#### Option 1: Send Plain JSON Messages (Recommended for Testing)

**Via Confluent Cloud Console**:
1. Go to Topics → Select topic → **Produce a new message**
2. Set **Value format** to **String** (not Avro)
3. Paste JSON payload:
```json
{
  "messageId": "test-001",
  "subject": "Test Notification",
  "body": "Test message",
  "recipient": "user@example.com",
  "timestamp": 1735574400000,
  "metadata": {"from": "test@example.com", "isHtml": "false"}
}
```

#### Option 2: Configure Avro Deserialization (Production)

See [Avro Setup Guide](./avro-setup.md) for complete instructions.

Quick steps:
1. Get Schema Registry credentials from Confluent Cloud
2. Add to `.env`:
```bash
KAFKA_SCHEMA_REGISTRY_URL=https://psrc-xxxxx.confluent.cloud
KAFKA_SCHEMA_REGISTRY_KEY=YOUR_KEY
KAFKA_SCHEMA_REGISTRY_SECRET=YOUR_SECRET
```
3. Rebuild and restart service

### Problem: Invalid Message Format

**Error**: `Invalid message received: MessageId=xxx, Topic=xxx`

**Common Causes**:
- Missing required fields (`messageId`, `subject`, `body`, `recipient`)
- Invalid email format in `recipient` field
- Malformed JSON syntax

**Solution**: Validate message against schema:
```json
{
  "messageId": "string (required)",
  "subject": "string (required)",
  "body": "string (required)",
  "recipient": "valid-email@example.com (required)",
  "timestamp": 1735574400000,
  "metadata": {
    "from": "sender@example.com",
    "isHtml": "false"
  }
}
```

Use [JSON validator](https://jsonlint.com) to check syntax.

## Connection Issues

### Problem: Authentication Failed

**Error**: `Broker: Topic authorization failed` or `Authentication failed`

**Solutions**:

1. **Verify API Key and Secret**:
```bash
podman compose exec notification-api env | grep KAFKA_SASL
```

2. **Check API Key Permissions** in Confluent Cloud:
   - Required for Consumer: READ, DESCRIBE
   - Required for Producer: WRITE, DESCRIBE, CREATE

3. **Rotate API Key** if compromised

### Problem: Topic Not Found

**Error**: `Subscribed topic not available`

**Solution**:
1. Create topics in Confluent Cloud Console
2. Verify topic names match configuration:
```bash
podman compose exec notification-api env | grep KAFKA
```

### Problem: Connection Timeout

**Error**: `Failed to resolve 'pkc-xxxxx.confluent.cloud'`

**Solutions**:
- Check network connectivity
- Verify DNS resolution
- Check firewall rules for port 9092
- Test connection:
```bash
telnet pkc-xxxxx.confluent.cloud 9092
```

## Schema Registry Issues

### Problem: Schema Not Found

**Error**: `Schema not found` or `404 Not Found`

**Solution**: Register schema in Schema Registry:

```bash
# Verify schema exists
curl -u "$SR_KEY:$SR_SECRET" \
  "$SR_URL/subjects/weather-alerts-value/versions"

# If missing, register from file
curl -X POST -H "Content-Type: application/vnd.schemaregistry.v1+json" \
  --data @schemas/avro/notification-message.avsc \
  -u "$SR_KEY:$SR_SECRET" \
  "$SR_URL/subjects/weather-alerts-value/versions"
```

### Problem: Schema Registry Authentication Failed

**Error**: `401 Unauthorized` when accessing Schema Registry

**Solution**:
1. Verify Schema Registry credentials are different from Kafka credentials
2. Check environment variables:
```bash
podman compose exec notification-api env | grep SCHEMA_REGISTRY
```

## Message Processing Issues

### Problem: Duplicate Messages

**Log**: `Duplicate message detected: MessageId=xxx`

**Explanation**: This is normal behavior for idempotency. The service tracks `messageId` to prevent duplicate processing.

**Action**: If duplicate rate is high (> 10%), check:
- Producer configuration (are retries too aggressive?)
- Network stability
- Consumer group state

### Problem: High Retry Rate

**Error**: `Failed to send email after retries: MessageId=xxx`

**Solutions**:
1. **Check SMTP Configuration**:
```bash
podman compose logs notification-api | grep -i smtp
```

2. **Test SMTP Connectivity**:
```bash
podman compose exec notification-api ping smtp.gmail.com
```

3. **Verify Email Credentials** in `appsettings.json`

### Problem: Kafka Lag

**Symptom**: Messages delayed or piling up

**Check Lag**:
```bash
# Via Confluent Cloud Console: Topics → Consumer Groups → notification-service

# Or via logs
podman compose logs notification-api | grep "partition" | tail -20
```

**Solutions**:
- Scale consumer instances
- Optimize message processing time
- Check for bottlenecks (SMTP, database)

## Diagnostic Commands

### Check Service Health

```bash
# Health check
curl http://localhost:8082/health

# View logs
podman compose logs -f notification-api

# Filter errors only
podman compose logs notification-api | grep -iE "error|exception"

# Track specific message
podman compose logs notification-api | grep "MessageId=test-001"
```

### Verify Configuration

```bash
# Kafka settings
podman compose exec notification-api env | grep KAFKA

# Schema Registry settings
podman compose exec notification-api env | grep SCHEMA_REGISTRY

# All environment variables
podman compose exec notification-api env
```

### Test Message Format

```bash
# Check first bytes of message (should start with 7B for JSON '{')
podman compose logs notification-api | grep "Message content preview"

# Example good JSON output:
# Message content preview (first bytes hex): 7B-22-6D-65-73, Length: 234

# Example Avro output (needs Avro consumer):
# Message content preview (first bytes hex): 00-00-00-00, Length: 156
```

## Quick Fixes

### Service Not Starting

```bash
podman compose build --no-cache notification-api
podman compose up -d notification-api
podman compose logs notification-api | head -50
```

### Reset Consumer Group (Caution: Re-processes All Messages)

```bash
# Stop service
podman compose stop notification-api

# Reset offsets via Confluent Cloud Console:
# Topics → Consumer Groups → notification-service → Reset offsets

# Restart service
podman compose up -d notification-api
```

### Clear Database (Fresh Start)

```bash
# Remove database file
rm notification-data/notifications.db

# Restart service (database will be recreated)
podman compose restart notification-api
```

## Common Error Patterns

| Error Pattern | Meaning | Solution |
|---------------|---------|----------|
| `0x00 is invalid` | Avro format, JSON consumer | Use Avro consumer or send JSON |
| `Authentication failed` | Wrong API credentials | Check `.env` file |
| `Topic authorization` | Missing permissions | Add READ/WRITE ACL in Confluent |
| `Topic not available` | Topic doesn't exist | Create topic in Confluent Cloud |
| `SMTP error` | Email sending failed | Check SMTP settings |
| `Database error` | SQLite issue | Check disk space, permissions |
| `Duplicate message` | Already processed | Normal idempotency behavior |

## Related Documentation

- [Confluent Cloud Setup](./confluent-cloud-setup.md)
- [Avro Setup](./avro-setup.md)
- [Message Schemas](../infrastructure/schemas.md)
- [NotificationService Monitoring](../services/notification-service.md)
