# Troubleshooting: Kafka Message Format Issues

## Problem: "0x00 is an invalid start of a value" Error

### Symptoms
```
System.Text.Json.JsonReaderException: '0x00' is an invalid start of a value
Failed to parse Kafka message, using raw content
Invalid message received: MessageId=xxx, Topic=general-events
```

### Root Cause

The NotificationService is configured to consume **plain JSON messages**, but the messages in Confluent Cloud are being sent in **Avro binary format** with Schema Registry encoding.

**What's happening:**
- Confluent Cloud Schema Registry prepends messages with a 5-byte header:
  - Byte 0: Magic byte `0x00` (indicates Schema Registry format)
  - Bytes 1-4: Schema ID (32-bit integer)
  - Bytes 5+: Avro-serialized payload

- The NotificationService `IConsumer<string, string>` expects UTF-8 JSON strings
- When it receives Avro binary data, the JSON parser fails on the `0x00` magic byte

## Solutions

You have **three options** to fix this issue:

---

### Option 1: Send Plain JSON Messages (Recommended for Testing)

**Best for:** Quick testing, simple setup, no Schema Registry needed

**How to send messages:**

#### Using Confluent Cloud Console
1. Go to your cluster → Topics → `general-events` or `weather-alerts`
2. Click **"Produce a new message"**
3. **Important:** Leave "Value format" as **String** (not Avro)
4. Paste the JSON payload:
   ```json
   {
     "messageId": "test-001",
     "subject": "Test Notification",
     "body": "This is a test message",
     "recipient": "user@example.com",
     "timestamp": 1735574400000,
     "metadata": {
       "from": "test@example.com",
       "isHtml": "false"
     }
   }
   ```
5. Click **"Produce"**

#### Using Confluent CLI
```bash
# Produce plain JSON (not Avro)
confluent kafka topic produce general-events \
  --cluster lkc-xxxxx \
  --api-key 4Z6OXHUL3JNA33WX \
  --api-secret cflt3xkPbgMSvkDaCTsRPwuEci6tYPulTiJsZrInd+7aPhrtxdEL31IxrwcLHuqQ

# Then paste your JSON and press Enter
```

**Verification:**
```bash
# Check logs - should see successful processing
docker-compose logs -f notification-api | grep "Notification sent successfully"
```

---

### Option 2: Update Consumer to Use Avro Deserialization

**Best for:** Production use with Schema Registry, type safety, schema evolution

**What you need:**
1. Confluent Schema Registry URL
2. Schema Registry API credentials
3. Update NotificationService to use Avro deserializer

**Implementation Steps:**

#### 1. Add NuGet Package
```xml
<PackageReference Include="Confluent.SchemaRegistry.Serdes.Avro" Version="2.3.0" />
```

#### 2. Update KafkaConsumerUtility.cs

Replace the consumer builder:

**Before:**
```csharp
_consumer = new ConsumerBuilder<string, string>(config).Build();
```

**After:**
```csharp
var schemaRegistryConfig = new SchemaRegistryConfig
{
    Url = configuration["Kafka:SchemaRegistryUrl"],
    BasicAuthUserInfo = $"{configuration["Kafka:SchemaRegistryKey"]}:{configuration["Kafka:SchemaRegistrySecret"]}"
};

using var schemaRegistry = new CachedSchemaRegistryClient(schemaRegistryConfig);

_consumer = new ConsumerBuilder<string, NotificationMessage>(config)
    .SetValueDeserializer(new AvroDeserializer<NotificationMessage>(schemaRegistry).AsSyncOverAsync())
    .Build();
```

#### 3. Update .env file
```bash
# Add Schema Registry credentials
KAFKA_SCHEMA_REGISTRY_URL=https://psrc-xxxxx.us-east-2.aws.confluent.cloud
KAFKA_SCHEMA_REGISTRY_KEY=YOUR_SR_API_KEY
KAFKA_SCHEMA_REGISTRY_SECRET=YOUR_SR_API_SECRET
```

#### 4. Update docker-compose.yml
```yaml
environment:
  - Kafka__SchemaRegistryUrl=${KAFKA_SCHEMA_REGISTRY_URL}
  - Kafka__SchemaRegistryKey=${KAFKA_SCHEMA_REGISTRY_KEY}
  - Kafka__SchemaRegistrySecret=${KAFKA_SCHEMA_REGISTRY_SECRET}
```

**Where to get Schema Registry credentials:**
1. Confluent Cloud Console → Environments → Your Environment
2. Schema Registry → API credentials → Create key

---

### Option 3: Configure Topic to Accept Both Formats

**Best for:** Migration period, supporting multiple producers

This approach allows the topic to accept both JSON and Avro messages.

**Steps:**

1. **Keep current consumer for JSON**
2. **Configure producers to send JSON** (not Avro)
3. **For Avro producers:** Use JSON serializer instead:

**Python example:**
```python
from confluent_kafka import Producer
import json

producer = Producer({
    'bootstrap.servers': 'pkc-921jm.us-east-2.aws.confluent.cloud:9092',
    'security.protocol': 'SASL_SSL',
    'sasl.mechanism': 'PLAIN',
    'sasl.username': '4Z6OXHUL3JNA33WX',
    'sasl.password': 'cflt3xkPbgMSvkDaCTsRPwuEci6tYPulTiJsZrInd+7aPhrtxdEL31IxrwcLHuqQ'
})

message = {
    "messageId": "test-001",
    "subject": "Test",
    "body": "Test message",
    "recipient": "user@example.com",
    "timestamp": 1735574400000,
    "metadata": {"from": "test@example.com"}
}

producer.produce(
    'general-events',
    key='test-001',
    value=json.dumps(message)  # JSON string, not Avro
)
producer.flush()
```

---

## Verification Steps

### 1. Check Message Format in Confluent Cloud

**Via Console:**
1. Go to Topics → Select your topic
2. Click "Messages" tab
3. Look at "Value format" column:
   - **String** = JSON (good for current consumer)
   - **Avro** = Binary with Schema Registry (requires Option 2)

### 2. Test with Sample Messages

Use the samples from `samples/notification-message-samples.json`:

```bash
# Send a test message (copy payload only)
curl -X POST http://localhost:8082/api/notifications/send \
  -H "Content-Type: application/json" \
  -d '{
    "messageId": "test-direct-001",
    "subject": "Direct API Test",
    "body": "Testing direct API",
    "recipient": "test@example.com",
    "timestamp": 1735574400000,
    "metadata": {"from": "test@example.com", "isHtml": "false"}
  }'
```

### 3. Monitor Logs for Success

**Successful processing looks like:**
```
[Debug] Consumed message from topic general-events, partition 0, offset 15
[Debug] Message content preview (first bytes hex): 7B-22-6D-65-73-73-61-67, Length: 234
[Information] Received message from topic general-events: MessageId=test-001
[Information] Created notification record with ID xxx for message test-001
[Information] Email sent successfully to test@example.com
[Information] Notification sent successfully: MessageId=test-001, Recipient=test@example.com
```

**Avro format detection:**
```
[Error] Message appears to be Avro-serialized with Schema Registry format (starts with 0x00 magic byte).
This consumer is configured for JSON messages.
Please ensure messages are sent as plain JSON, not Avro.
Topic: general-events, Message length: 156
```

## Recommendation

**For your current setup, use Option 1** (Plain JSON):

✅ **Pros:**
- Works immediately with current code
- No additional dependencies
- Easy to test and debug
- Sample messages are already in JSON format

❌ **Cons:**
- No schema validation
- No schema evolution support
- Slightly larger message size

**For production, consider Option 2** (Avro with Schema Registry):

✅ **Pros:**
- Schema validation at produce/consume time
- Schema evolution support
- Smaller message size (binary)
- Type safety

❌ **Cons:**
- Requires Schema Registry setup
- Additional cost (Schema Registry)
- More complex configuration

## Quick Fix Right Now

**To get your system working immediately:**

1. **Delete existing messages** in Confluent Cloud topics (if they're Avro)
2. **Send new messages as plain JSON** using Confluent Cloud Console:
   - Go to Topics → general-events → Produce message
   - Set "Value format" to **String** (not Avro)
   - Paste JSON from `samples/notification-message-samples.json`
3. **Monitor logs:**
   ```bash
   docker-compose logs -f notification-api
   ```
4. **Look for success message:**
   ```
   Notification sent successfully: MessageId=xxx, Recipient=xxx
   ```

## Additional Resources

- **Sample JSON Messages**: [samples/notification-message-samples.json](samples/notification-message-samples.json)
- **Avro Schemas**: [schemas/avro/notification-message.avsc](schemas/avro/notification-message.avsc)
- **Confluent Cloud Setup**: [QUICK_START_CONFLUENT.md](QUICK_START_CONFLUENT.md)
- **Observability Guide**: [NotificationService/OBSERVABILITY.md](NotificationService/OBSERVABILITY.md)

## Still Having Issues?

Check the enhanced error logs - they now detect Avro format and provide specific guidance:

```bash
# View recent errors with context
docker-compose logs --tail=50 notification-api | grep -A 5 -B 5 "0x00"

# Check message format
docker-compose logs notification-api | grep "Message content preview"
```

The new diagnostic logging will show:
- First bytes of the message in hex format
- Detection of Avro magic byte (0x00)
- Specific error message with resolution steps
