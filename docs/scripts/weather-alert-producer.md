# Weather Alert Producer Scripts

Python scripts for sending weather alert messages to Confluent Cloud Kafka topics.

## Quick Start

### Method 1: Confluent Cloud Console (Easiest - No Setup)

1. Go to https://confluent.cloud
2. Navigate to **Topics** → `weather-alerts` → **Messages** → **"Produce a new message"**
3. Set **Format** to **Avro** and **Schema** to `weather-alerts-value` version 15
4. Paste test message:

```json
{
  "messageId": "quickstart-001",
  "subject": "Test Weather Alert",
  "body": "",
  "recipient": "your-email@example.com",
  "timestamp": 1735689600000,
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
    "windSpeed": 65.0,
    "precipitation": 35.2
  },
  "metadata": {
    "from": "alerts@weatherapp.com",
    "priority": "high"
  }
}
```

5. Click **Produce**
6. Check your email for the HTML weather alert

### Method 2: Python Producer Script

**Prerequisites**:
```bash
pip install 'confluent-kafka[avro]'
```

**Send Messages**:
```bash
cd /Users/ghostair/Projects/containerApp

# Send 1 CRITICAL message
python3 scripts/produce-weather-alerts.py --severity CRITICAL

# Send all 5 samples
python3 scripts/produce-weather-alerts.py --all

# Interactive mode
python3 scripts/produce-weather-alerts.py --interactive

# Send random messages
python3 scripts/produce-weather-alerts.py --count 3
```

## Configuration

Ensure `.env` file contains:

```bash
# Confluent Cloud Kafka
KAFKA_BOOTSTRAP_SERVERS=pkc-xxxxx.us-east-2.aws.confluent.cloud:9092
KAFKA_SECURITY_PROTOCOL=SaslSsl
KAFKA_SASL_MECHANISM=Plain
KAFKA_SASL_USERNAME=YOUR_API_KEY
KAFKA_SASL_PASSWORD=YOUR_API_SECRET

# Schema Registry
KAFKA_SCHEMA_REGISTRY_URL=https://psrc-xxxxx.confluent.cloud
KAFKA_SCHEMA_REGISTRY_KEY=YOUR_SR_KEY
KAFKA_SCHEMA_REGISTRY_SECRET=YOUR_SR_SECRET
```

## Sample Messages

The script includes 5 pre-configured samples:

### 1. CRITICAL - Extreme Heat (Phoenix, AZ)
- Temperature: 48.0°C (118.4°F)
- Alert Type: TEMPERATURE_EXTREME
- Header Color: Red (#DC2626)

### 2. SEVERE - Thunderstorm (San Francisco, CA)
- Conditions: Thunderstorm with heavy hail
- Wind: 65.0 km/h, Precipitation: 35.2 mm
- Header Color: Orange (#EA580C)

### 3. WARNING - Winter Storm (Denver, CO)
- Temperature: -8.0°C, Heavy snow
- Precipitation: 15.8 mm
- Header Color: Amber (#D97706)

### 4. WARNING - High Winds (Chicago, IL)
- Wind Speed: 88.5 km/h (55 mph)
- Conditions: Overcast with gusty winds
- Header Color: Amber (#D97706)

### 5. INFO - Daily Forecast (Seattle, WA)
- Conditions: Partly cloudy with light rain
- Temperature: 18.5°C
- Header Color: Blue (#2563EB)

## Script Options

```bash
# Specific severity
python3 scripts/produce-weather-alerts.py --severity CRITICAL
python3 scripts/produce-weather-alerts.py --severity SEVERE
python3 scripts/produce-weather-alerts.py --severity WARNING
python3 scripts/produce-weather-alerts.py --severity INFO

# Custom topic
python3 scripts/produce-weather-alerts.py --topic custom-topic

# Custom .env file
python3 scripts/produce-weather-alerts.py --env /path/to/.env

# Count
python3 scripts/produce-weather-alerts.py --count 10
```

## How It Works

1. **Loads Configuration**: Reads credentials from `.env`
2. **Loads Avro Schema**: Reads `weather-alert.avsc`
3. **Creates Producer**: Initializes Kafka producer with Schema Registry
4. **Serializes Messages**: Uses Avro serializer
5. **Sends to Topic**: Produces to `weather-alerts`
6. **Confirms Delivery**: Reports partition and offset

## Monitoring

### Watch Service Logs

```bash
podman compose logs -f notification-api | grep -E "(Successfully deserialized|Email sent)"
```

**Expected Output**:
```
✅ Successfully deserialized WeatherAlert message: MessageId=test-critical-xxx
✅ Email sent successfully to: user@example.com
```

### Check Database

```bash
podman exec -it containerapp-notification-api-1 sqlite3 /app/data/notifications.db \
  "SELECT MessageId, Subject, Status, CreatedAt FROM Notifications ORDER BY CreatedAt DESC LIMIT 5;"
```

## Email Output

Recipients receive professional HTML emails with:
- **Colored header** based on severity (Red/Orange/Amber/Blue)
- **Location details** (City, State, ZIP)
- **Weather conditions** with metric/imperial conversions
- **Responsive mobile-friendly design**

Example:
```
🔴 CRITICAL: Extreme Heat Emergency - Phoenix
Severity: CRITICAL - Temperature Extreme
Location: Phoenix, Arizona 85001
Temperature: 48.0°C (118.4°F)
Conditions: Clear sky - Extreme heat advisory
Wind Speed: 8.0 km/h (5.0 mph)
Precipitation: 0.0 mm
```

## Troubleshooting

### TOPIC_AUTHORIZATION_FAILED

**Error**: `Broker: Topic authorization failed`

**Cause**: API key lacks WRITE permissions

**Solutions**:

1. **Add Permissions** (Recommended):
   - Go to Confluent Cloud → API Keys
   - Select your key → ACL → Add permissions
   - Resource: Topic `weather-alerts`
   - Operations: WRITE, DESCRIBE, CREATE

2. **Create New API Key** with full permissions

3. **Use Method 1** (Console) - no permissions needed

### Import Error

**Error**: `confluent-kafka package not found`

**Solution**:
```bash
pip install 'confluent-kafka[avro]'
```

### Authentication Error

**Error**: `Authentication failed`

**Solution**: Check credentials in `.env`:
```bash
grep KAFKA .env
```

### Schema Registry Error

**Error**: `Schema not found`

**Solution**: Verify schema is registered:
```bash
curl -u "$SR_KEY:$SR_SECRET" \
  "$SR_URL/subjects/weather-alerts-value/versions"
```

### No Email Received

**Checks**:
1. SMTP configured in NotificationService
2. Check spam folder
3. Verify recipient email address
4. Check service logs: `podman compose logs notification-api | grep -i smtp`

## Advanced Usage

### Custom Message

```python
from produce_weather_alerts import create_producer, send_message

custom_message = {
    "messageId": "custom-001",
    "subject": "Custom Alert",
    "body": "",
    "recipient": "user@example.com",
    "timestamp": int(time.time() * 1000),
    "alertType": "GENERAL_ALERT",
    "severity": "INFO",
    "location": {
        "zipCode": "10001",
        "city": "New York",
        "state": "New York",
        "latitude": 40.7128,
        "longitude": -74.0060
    },
    "weatherConditions": {
        "currentTemperature": 20.0,
        "weatherCode": 1,
        "weatherDescription": "Sunny",
        "windSpeed": 10.0,
        "precipitation": 0.0
    },
    "metadata": {"from": "custom@example.com"}
}

producer = create_producer(env_vars)
send_message(producer, custom_message)
producer.flush()
```

## Verify Success

Check all indicators:
- [ ] Message delivered to topic (partition + offset shown)
- [ ] Service logs show "Successfully deserialized"
- [ ] Service logs show "Email sent successfully"
- [ ] Email received with colored header and weather data
- [ ] Database shows notification with Status="Sent"

## Related Documentation

- [Message Schemas](../infrastructure/schemas.md)
- [Kafka Troubleshooting](../kafka/troubleshooting.md)
- [NotificationService Monitoring](../services/notification-service.md)
