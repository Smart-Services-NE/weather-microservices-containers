# Send Test Message - Step-by-Step Guide

## Quick Steps to Send Your First JSON Message

### 1. Access Confluent Cloud Console

1. Open your browser and go to: **https://confluent.cloud**
2. Log in to your account
3. Navigate to your cluster (the one with bootstrap server `pkc-921jm.us-east-2.aws.confluent.cloud`)

### 2. Navigate to Topics

1. In the left sidebar, click **"Topics"**
2. You'll see your topics listed:
   - `weather-alerts`
   - `general-events`
3. Click on **`general-events`** (easier to test with)

### 3. Produce a Message

1. Click the **"Messages"** tab
2. Click **"+ Produce a new message to this topic"** button

### 4. CRITICAL: Configure Message Format

**Before entering any data, configure the format:**

1. Look for **"Value format"** dropdown (usually near the top)
2. **IMPORTANT**: Select **"String"** from the dropdown
   - ❌ DO NOT use "Avro"
   - ❌ DO NOT use "JSON Schema"
   - ❌ DO NOT use "Protobuf"
   - ✅ USE "String"

### 5. Enter Message Content

In the **"Value"** text box, paste this JSON (copy the entire block):

```json
{
  "messageId": "test-manual-001",
  "subject": "🎉 First Successful JSON Test",
  "body": "Congratulations! This message was sent as plain JSON and should be processed successfully by the NotificationService. You should receive this email if SMTP is configured.",
  "recipient": "YOUR_EMAIL@example.com",
  "timestamp": 1735574400000,
  "metadata": {
    "from": "success@weatherapp.com",
    "isHtml": "false",
    "priority": "high",
    "testId": "manual-test-001"
  }
}
```

**IMPORTANT**: Replace `YOUR_EMAIL@example.com` with your actual email address!

### 6. Optional: Set Message Key

In the **"Key"** field (optional), you can enter:
```
test-manual-001
```

### 7. Produce the Message

1. Click **"Produce"** button
2. You should see a success message: "Message produced successfully"

### 8. Monitor the Logs

Open your terminal and run:

```bash
docker-compose logs -f notification-api | grep -A 3 -B 3 "test-manual-001"
```

### 9. Expected Success Output

You should see logs like this within a few seconds:

```
[Debug] Consumed message from topic general-events, partition X, offset Y
[Debug] Message content preview (first bytes hex): 7B-22-6D-65-73-73-61-67-65-49-64, Length: 387
[Information] Received message from topic general-events: MessageId=test-manual-001
[Information] Created notification record with ID xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx for message test-manual-001
[Information] Email sent successfully to YOUR_EMAIL@example.com
[Information] Updated notification record with ID xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
[Information] Notification sent successfully: MessageId=test-manual-001, Recipient=YOUR_EMAIL@example.com
[Information] Successfully processed and sent notification for MessageId=test-manual-001
```

**Key indicators of success:**
- ✅ Hex preview starts with `7B` (ASCII for `{` - JSON opening brace)
- ✅ "Email sent successfully"
- ✅ "Notification sent successfully"
- ✅ NO errors about Avro or 0x00 magic byte

### 10. Check Your Email

If you configured SMTP credentials, you should receive an email with:
- **From**: success@weatherapp.com
- **Subject**: 🎉 First Successful JSON Test
- **Body**: Congratulations message

---

## Common Mistakes to Avoid

### ❌ Mistake #1: Using Avro Format
**Problem**: Value format is set to "Avro" instead of "String"

**Error you'll see**:
```
Message appears to be Avro-serialized with Schema Registry format (starts with 0x00 magic byte)
```

**Fix**: Change "Value format" dropdown to "String"

---

### ❌ Mistake #2: Invalid JSON Syntax
**Problem**: Missing comma, extra comma, or quotes not closed

**Error you'll see**:
```
Failed to parse Kafka message as JSON
```

**Fix**: Use a JSON validator (https://jsonlint.com) to check your JSON before pasting

---

### ❌ Mistake #3: Missing Required Fields
**Problem**: JSON is missing `messageId`, `subject`, `body`, or `recipient`

**Error you'll see**:
```
Invalid message received: MessageId=xxx, Topic=general-events
```

**Fix**: Ensure all required fields are present:
- `messageId` (string)
- `subject` (string)
- `body` (string)
- `recipient` (string, valid email format)
- `timestamp` (number, Unix milliseconds)

---

### ❌ Mistake #4: Invalid Email Format
**Problem**: Recipient is not a valid email address

**Error you'll see**:
```
Invalid message received: MessageId=xxx, Topic=general-events
```

**Fix**: Use proper email format: `user@domain.com`

---

## More Test Samples

Once your first message works, try these:

### Simple Text Notification
```json
{
  "messageId": "simple-test-001",
  "subject": "Simple Test",
  "body": "This is a plain text test message.",
  "recipient": "YOUR_EMAIL@example.com",
  "timestamp": 1735574400000,
  "metadata": {
    "from": "test@weatherapp.com",
    "isHtml": "false"
  }
}
```

### HTML Email Notification
```json
{
  "messageId": "html-test-001",
  "subject": "HTML Test Email",
  "body": "<html><body><h1>Success!</h1><p>This is an <strong>HTML</strong> formatted email.</p><ul><li>Item 1</li><li>Item 2</li></ul></body></html>",
  "recipient": "YOUR_EMAIL@example.com",
  "timestamp": 1735574400000,
  "metadata": {
    "from": "test@weatherapp.com",
    "isHtml": "true"
  }
}
```

### Weather Alert Notification
```json
{
  "messageId": "weather-alert-test-001",
  "subject": "⚠️ Test Weather Alert",
  "body": "This is a test weather alert notification. Severe thunderstorm warning in effect for your area until 8:00 PM. Take shelter immediately.",
  "recipient": "YOUR_EMAIL@example.com",
  "timestamp": 1735574400000,
  "metadata": {
    "from": "alerts@weatherapp.com",
    "isHtml": "false",
    "alertType": "SEVERE_WEATHER",
    "severity": "WARNING",
    "priority": "high"
  }
}
```

**Remember**: Send these to the appropriate topic:
- `general-events` → General notifications, subscriptions, forecasts
- `weather-alerts` → Weather warnings and alerts

---

## Troubleshooting

### Issue: "Message produced successfully" but no logs

**Check:**
```bash
# Verify service is running
docker-compose ps notification-api

# Check if consumer is subscribed
docker-compose logs notification-api | grep "Subscribed to topics"

# Check for any errors
docker-compose logs notification-api | grep -i error | tail -20
```

### Issue: Still seeing Avro errors

**Verify Value Format:**
1. Go back to Confluent Cloud Console
2. When producing message, double-check dropdown shows **"String"**
3. NOT "Avro", NOT "JSON Schema", NOT "Protobuf"

### Issue: Email not sent

**Check SMTP Configuration:**
```bash
# View current config (from inside container)
docker-compose exec notification-api env | grep Email

# Check logs for SMTP errors
docker-compose logs notification-api | grep -i smtp
```

**Note**: If SMTP is not configured, the message will still be processed and saved to the database, but no email will be sent.

---

## Next Steps After Success

Once you successfully send and process a JSON message:

1. ✅ **Verify in Database**:
   ```bash
   # View pending notifications
   curl http://localhost:8082/api/notifications/pending
   ```

2. ✅ **Check Zipkin Traces**:
   - Open http://localhost:9411
   - Search for service: `NotificationService`
   - Find your `messageId` in traces

3. ✅ **View Prometheus Metrics**:
   - Open http://localhost:9090
   - Query: `notification_sent_total`
   - Should show increment

4. ✅ **Send More Samples**:
   - Use samples from [samples/notification-message-samples.json](samples/notification-message-samples.json)
   - Test different scenarios (HTML, weather alerts, etc.)

5. ✅ **Integrate with Your Application**:
   - Use the JSON format in your producer code
   - See [TROUBLESHOOTING_KAFKA_JSON.md](TROUBLESHOOTING_KAFKA_JSON.md) for Python/C# examples

---

## Need More Help?

- **Full Sample Library**: [samples/notification-message-samples.json](samples/notification-message-samples.json)
- **Format Troubleshooting**: [TROUBLESHOOTING_KAFKA_JSON.md](TROUBLESHOOTING_KAFKA_JSON.md)
- **Observability Guide**: [NotificationService/OBSERVABILITY.md](NotificationService/OBSERVABILITY.md)
- **Quick Monitoring**: [NotificationService/MONITORING_QUICK_REF.md](NotificationService/MONITORING_QUICK_REF.md)

Good luck! 🚀
