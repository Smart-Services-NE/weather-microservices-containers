# Get Schema Registry Credentials from Confluent Cloud

## Why You Need This

Your Confluent Cloud topics are configured with **Avro schemas**, so the NotificationService needs to connect to the **Schema Registry** to deserialize messages properly.

I've already implemented the Avro deserialization support. You just need to get your credentials.

## Step-by-Step Instructions

### 1. Log into Confluent Cloud Console

1. Go to https://confluent.cloud
2. Log in with your credentials
3. Select your **Environment** (where your Kafka cluster is)

### 2. Navigate to Schema Registry

**Option A: Via Left Sidebar**
1. Click **"Schema Registry"** in the left sidebar
2. You'll see your Schema Registry endpoint

**Option B: Via Environments**
1. Click **"Environments"** in the left sidebar
2. Click on your environment name
3. Click **"Schema Registry"** tab

### 3. Get Schema Registry URL

You should see something like:
```
https://psrc-xxxxx.us-east-2.aws.confluent.cloud
```

**Copy this URL** - you'll need it for your `.env` file.

### 4. Create API Credentials for Schema Registry

1. On the Schema Registry page, find **"API credentials"** section
2. Click **"+ Add key"** or **"Create API key"**
3. Enter a description: `NotificationService Consumer`
4. Click **"Create"**

### 5. Save Your Credentials

You'll see:
- **API Key**: Something like `AABBCCDDEE123456`
- **API Secret**: A long string like `abcdef1234567890ABCDEF1234567890abcdef1234567890ABCDEF1234567890`

**IMPORTANT**:
- ⚠️ Copy both immediately - the secret will only be shown once!
- ⚠️ Store them securely
- ⚠️ Don't share them or commit them to version control

### 6. Update Your `.env` File

Open `/Users/ghostair/Projects/containerApp/.env` and update these lines:

```bash
# Confluent Cloud Schema Registry Configuration
KAFKA_SCHEMA_REGISTRY_URL=https://psrc-xxxxx.us-east-2.aws.confluent.cloud
KAFKA_SCHEMA_REGISTRY_KEY=YOUR_ACTUAL_API_KEY_HERE
KAFKA_SCHEMA_REGISTRY_SECRET=YOUR_ACTUAL_API_SECRET_HERE
```

**Example** (with fake credentials):
```bash
KAFKA_SCHEMA_REGISTRY_URL=https://psrc-12abc.us-east-2.aws.confluent.cloud
KAFKA_SCHEMA_REGISTRY_KEY=AABBCCDDEE123456
KAFKA_SCHEMA_REGISTRY_SECRET=abcdef1234567890ABCDEF1234567890abcdef1234567890ABCDEF1234567890
```

### 7. Rebuild and Restart

```bash
# Rebuild with Avro support
docker-compose build --no-cache notification-api

# Restart the service
docker-compose up -d notification-api

# Check logs for successful Schema Registry connection
docker-compose logs -f notification-api | head -30
```

### 8. Verify Success

Look for these log messages:

✅ **Success:**
```
[Information] Schema Registry configured with authentication
[Information] Avro Kafka Consumer initialized with Schema Registry: https://psrc-xxxxx...
[Information] Subscribed to Kafka topics: weather-alerts, general-events
[Information] Successfully deserialized Avro message: MessageId=xxx, Subject=xxx
```

❌ **If you see errors:**
```
Schema Registry URL is required for Avro deserialization
```
- Check that your `.env` file has the correct URL

```
401 Unauthorized
```
- Check your API Key and Secret are correct

```
Schema not found
```
- Verify your topics have registered schemas in Schema Registry

---

## Alternative: Find Schema Registry via CLI

If you have Confluent CLI installed:

```bash
# List Schema Registry clusters
confluent schema-registry cluster describe

# Get API endpoint
confluent schema-registry cluster describe --output json | jq -r '.endpoint_url'
```

---

## What Happens After Configuration

Once configured correctly:

1. **NotificationService will automatically use Avro deserialization**
   - No code changes needed
   - The service detects Schema Registry URL and switches mode

2. **Messages are deserialized using registered schemas**
   - Automatic schema validation
   - Type-safe deserialization
   - Schema evolution support

3. **You'll see different log messages**:
   - "Successfully deserialized Avro message" (instead of JSON parse errors)
   - No more "0x00 magic byte" errors
   - Messages process successfully

---

## Security Best Practices

✅ **DO:**
- Store credentials in `.env` file (already in `.gitignore`)
- Use API keys with minimal required permissions
- Rotate API keys periodically
- Use different keys for dev/staging/prod

❌ **DON'T:**
- Commit credentials to git
- Share credentials in Slack/email
- Use production keys in development
- Hardcode credentials in source files

---

## Troubleshooting

### Issue: Can't find Schema Registry in Console

**Check:**
1. Make sure you're in the correct Environment
2. Some accounts might not have Schema Registry enabled by default
3. Contact Confluent support if Schema Registry option is missing

### Issue: Schema Registry shows "Not enabled"

**Solution:**
1. Click "Enable Schema Registry"
2. Choose cloud provider and region (should match your Kafka cluster)
3. Wait for provisioning (2-3 minutes)

### Issue: API Key creation fails

**Check:**
- You have sufficient permissions in Confluent Cloud
- Your account is not at API key limit
- Try creating from a different browser/session

---

## Next Steps

After configuring Schema Registry credentials:

1. ✅ Rebuild and restart NotificationService
2. ✅ Check logs for successful Avro deserialization
3. ✅ Existing Avro messages in your topics will now be processed correctly
4. ✅ Monitor metrics and traces in Prometheus/Zipkin

You're all set! The NotificationService is now fully compatible with your Avro-configured Confluent Cloud topics. 🎉
