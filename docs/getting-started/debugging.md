# Debugging Guide

How to debug containerized .NET applications in this project.

## Prerequisites

- VS Code with C# Dev Kit and Docker extensions
- Docker Desktop running

## Option 1: Remote Debugging (Recommended)

Debug inside the running container - best for production-like environment.

### Steps

1. **Rebuild in debug mode**:
```bash
docker-compose -f docker-compose.yml -f docker-compose.debug.yml up -d --build notification-api
```

2. **Open VS Code** in project root:
```bash
code /Users/ghostair/Projects/containerApp
```

3. **Set breakpoints**:
   - Open file (e.g., `NotificationService/Utilities/KafkaConsumerUtility.cs`)
   - Click left margin to set breakpoints

4. **Attach debugger**:
   - Press `F5` or "Run and Debug" in sidebar
   - Select "Docker: Attach to NotificationService"
   - Select the `dotnet` process

5. **Trigger code**:
   - Send Kafka message
   - Debugger pauses at breakpoints
   - Use F10 (step over), F11 (step into)

6. **Inspect variables**:
   - Hover over variables
   - Use "Variables" panel
   - Use "Debug Console" for expressions

### Send Test Message

```bash
docker exec -i containerapp-kafka-1 kafka-console-producer \
  --broker-list kafka:9093 \
  --topic weather-alerts <<'EOF'
{"messageId":"debug-001","subject":"Debug Test","body":"Testing breakpoints","recipient":"debug@example.com","timestamp":1735574400000}
EOF
```

### Stop Debug Mode

```bash
docker-compose down notification-api
docker-compose up -d notification-api
```

## Option 2: Local Debugging

Run application directly on your machine (not in Docker) - fastest for development.

### Steps

1. **Stop containerized service**:
```bash
docker-compose stop notification-api notification-api-dapr
```

2. **Ensure Kafka is running**:
```bash
docker-compose ps kafka
```

3. **Set breakpoints** in VS Code

4. **Start debugging**:
   - Press `F5`
   - Select ".NET Core Launch (NotificationService - Local)"
   - App starts on `http://localhost:8082`

5. **Send test messages** (Kafka still in Docker)

### Advantages

- Fast rebuild times
- Instant debugging
- Hot reload support

### Disadvantages

- Requires local .NET 10 SDK
- No Dapr sidecar (service-to-service calls won't work)
- Environment may differ from production

## Option 3: Console Logging

Add detailed logs without using a debugger.

### Steps

1. **Edit `appsettings.json`**:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "NotificationService": "Trace",
      "Confluent.Kafka": "Information"
    }
  }
}
```

2. **Add log statements**:
```csharp
_logger.LogDebug("Parsing message: {MessageContent}", messageContent);
_logger.LogInformation("Message parsed: MessageId={MessageId}", messageId);
```

3. **Rebuild and restart**:
```bash
docker-compose build notification-api
docker-compose up -d notification-api
```

4. **View logs**:
```bash
docker-compose logs -f notification-api
```

## Debugging Specific Scenarios

### Kafka Message Parsing

**Breakpoint locations** in `KafkaConsumerUtility.cs`:
- Line 60: Message consumed from Kafka
- Line 73: Inspect raw message content
- Line 87: Before parsing
- Line 107: JSON parsing
- Line 149: Return parsed message

**Variables to inspect**:
- `consumeResult.Message.Value` - Raw message
- `messageContent` - Message content
- `topic` - Kafka topic
- `jsonDoc.RootElement` - Parsed JSON
- `notificationMessage` - Final result

### Message Processing

**Breakpoints** in `NotificationManager.cs`:
- Start of `ProcessNotificationAsync`
- Validation logic
- Email sending
- Database persistence

### Email Sending

**Breakpoints** in `EmailAccessor.cs`:
- SMTP client creation
- Email message construction
- `SendMailAsync` call

## Troubleshooting

### Debugger Won't Attach

**Error**: "Could not attach to process"

**Solutions**:
1. Ensure container built with `Dockerfile.debug`
2. Check container is running: `docker ps | grep notification`
3. Verify process exists: `docker exec -it containerapp-notification-api-1 ps aux`
4. Rebuild: `docker-compose -f docker-compose.yml -f docker-compose.debug.yml up -d --build notification-api`

### Breakpoints Not Hitting

**Issue**: Breakpoints show as hollow circles

**Solutions**:
1. Ensure building in **Debug** mode (check `Dockerfile.debug`)
2. Verify source mapping in `.vscode/launch.json`: `"/src": "${workspaceRoot}"`
3. Rebuild completely:
```bash
docker-compose down
docker-compose -f docker-compose.yml -f docker-compose.debug.yml up -d --build notification-api
```

### Source Code Doesn't Match

**Error**: "Source code is different from compiled code"

**Solution**: Container source is stale, rebuild:
```bash
docker-compose build notification-api --no-cache
```

### Local Debugging Can't Connect to Kafka

**Error**: "Connection refused to localhost:9092"

**Solution**: The `.vscode/launch.json` should have `Kafka__BootstrapServers=localhost:9092`. Verify Kafka is accessible:
```bash
docker-compose ps kafka
telnet localhost 9092
```

## Quick Reference

### Common Debugging Commands

```bash
# Start debug mode
docker-compose -f docker-compose.yml -f docker-compose.debug.yml up -d --build notification-api

# View logs
docker-compose logs -f notification-api

# Send test JSON message
docker exec -i containerapp-kafka-1 kafka-console-producer --broker-list kafka:9093 --topic weather-alerts <<< '{"messageId":"test-001","subject":"Test","body":"Test message","recipient":"test@example.com","timestamp":1735574400000}'

# Check container processes
docker exec -it containerapp-notification-api-1 ps aux

# Restart in normal mode
docker-compose down notification-api
docker-compose up -d notification-api

# View Kafka topics
docker exec -it containerapp-kafka-1 kafka-topics --list --bootstrap-server kafka:9093

# Consume messages from topic
docker exec -it containerapp-kafka-1 kafka-console-consumer --bootstrap-server kafka:9093 --topic weather-alerts --from-beginning --max-messages 5
```

## Recommended Workflow

1. **Start with remote debugging** (Option 1) for production-like environment
2. **Use local debugging** (Option 2) for fast iteration during development
3. **Add strategic log statements** (Option 3) for production monitoring

**Pro tip**: Keep two terminal windows open:
- Terminal 1: Docker commands and log viewing
- Terminal 2: Kafka message sending for testing

## Related Documentation

- [NotificationService Monitoring](../services/notification-service.md)
- [Kafka Troubleshooting](../kafka/troubleshooting.md)
- [Quick Start Guide](./quick-start.md)
