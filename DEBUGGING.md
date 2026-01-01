# Debugging Guide for ContainerApp

This guide explains how to debug the containerized .NET applications in this project.

## Prerequisites

1. **VS Code** with the following extensions:
   - C# Dev Kit (Microsoft)
   - Docker (Microsoft)

2. **Docker Desktop** running

## Option 1: Remote Debugging (Attach to Container)

This is the **recommended approach** for debugging containerized applications.

### Steps:

1. **Rebuild and start the service in debug mode:**

   ```bash
   docker-compose -f docker-compose.yml -f docker-compose.debug.yml up -d --build notification-api
   ```

2. **Open VS Code** in the project root:

   ```bash
   code /Users/ghostair/Projects/containerApp
   ```

3. **Set breakpoints** in your code:
   - Open any file (e.g., `NotificationService/Utilities/KafkaConsumerUtility.cs`)
   - Click in the left margin next to a line number to set a breakpoint (red dot will appear)
   - Example locations:
     - Line 60: `var consumeResult = _consumer.Consume(cancellationToken);`
     - Line 87: `var notificationMessage = ParseKafkaMessage(topic, messageContent);`
     - Line 107: `using var jsonDoc = System.Text.Json.JsonDocument.Parse(messageContent);`

4. **Attach the debugger:**
   - Press `F5` or click "Run and Debug" in the sidebar
   - Select "Docker: Attach to NotificationService"
   - Select the `dotnet` process (usually the one with the lowest PID)

5. **Trigger the code:**
   - Send a Kafka message (see below)
   - The debugger will pause at your breakpoints
   - Use the debug toolbar to step through code (F10 = step over, F11 = step into)

6. **Inspect variables:**
   - Hover over variables to see their values
   - Use the "Variables" panel on the left
   - Use the "Debug Console" at the bottom to evaluate expressions

### Sending Test Messages

While debugging, send a JSON message to trigger your breakpoints:

```bash
# Send JSON message to weather-alerts topic
docker exec -i containerapp-kafka-1 kafka-console-producer \
  --broker-list kafka:9093 \
  --topic weather-alerts <<'EOF'
{"messageId":"debug-test-001","subject":"Debug Test Alert","body":"Testing breakpoints","recipient":"debug@example.com","timestamp":1735574400000,"metadata":{"from":"debug@test.com","isHtml":"false"}}
EOF
```

### Viewing Logs While Debugging

In a separate terminal, watch the logs:

```bash
docker-compose logs -f notification-api
```

### Stopping Debug Mode

```bash
# Stop the debug container
docker-compose down notification-api

# Restart in normal mode
docker-compose up -d notification-api
```

---

## Option 2: Local Debugging (Run Outside Container)

This runs the application **directly on your machine** (not in Docker), which is faster for debugging but requires local .NET SDK.

### Steps:

1. **Stop the containerized service:**

   ```bash
   docker-compose stop notification-api notification-api-dapr
   ```

2. **Ensure Kafka is still running:**

   ```bash
   docker-compose ps kafka
   ```

3. **Open VS Code** and set breakpoints in your code

4. **Start debugging:**
   - Press `F5` or click "Run and Debug"
   - Select ".NET Core Launch (NotificationService - Local)"

5. **The app will start locally** on `http://localhost:8082`
   - Debugger is attached automatically
   - Breakpoints will trigger
   - You can step through code, inspect variables, etc.

6. **Send test messages** using the command above (Kafka is still in Docker)

### Advantages:
- Faster rebuild times (no Docker image rebuild)
- Instant debugging (no container attach needed)
- Hot reload support

### Disadvantages:
- Requires local .NET 10 SDK
- No Dapr sidecar (service-to-service calls won't work)
- Environment may differ from production

---

## Option 3: Console Logging (No Debugger)

If you just want more detailed logs without using a debugger:

1. **Edit `appsettings.json`** to increase log level:

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

2. **Add log statements** in your code:

   ```csharp
   _logger.LogDebug("Parsing message: {MessageContent}", messageContent);
   _logger.LogInformation("Message parsed successfully: MessageId={MessageId}", messageId);
   ```

3. **Rebuild and restart:**

   ```bash
   docker-compose build notification-api
   docker-compose up -d notification-api
   ```

4. **View logs:**

   ```bash
   docker-compose logs -f notification-api
   ```

---

## Debugging Specific Scenarios

### Debugging Kafka Message Parsing

**Breakpoint locations in KafkaConsumerUtility.cs:**

- Line 60: When message is consumed from Kafka
- Line 73: To inspect raw message content
- Line 87: Before parsing begins
- Line 107: JSON parsing start
- Line 149: Return parsed NotificationMessage

**Variables to inspect:**
- `consumeResult.Message.Value` - Raw message string
- `messageContent` - Message content
- `topic` - Kafka topic name
- `jsonDoc.RootElement` - Parsed JSON document
- `notificationMessage` - Final parsed result

### Debugging Message Processing

**Breakpoint locations in NotificationManager:**

1. Find the NotificationManager file:
   ```bash
   find NotificationService -name "*NotificationManager.cs"
   ```

2. Set breakpoints at:
   - Start of `ProcessNotificationAsync` method
   - Validation logic
   - Email sending logic
   - Database persistence logic

### Debugging Email Sending

**Breakpoint locations in EmailAccessor:**

1. Find the EmailAccessor file:
   ```bash
   find NotificationService -name "*EmailAccessor.cs"
   ```

2. Set breakpoints at:
   - SMTP client creation
   - Email message construction
   - `SendMailAsync` call

---

## Troubleshooting

### Debugger won't attach

**Issue**: "Could not attach to process"

**Solution**:
1. Ensure the container is built with `Dockerfile.debug` (has vsdbg)
2. Check container is running: `docker ps | grep notification`
3. Verify process exists: `docker exec -it containerapp-notification-api-1 ps aux`
4. Try rebuilding: `docker-compose -f docker-compose.yml -f docker-compose.debug.yml up -d --build notification-api`

### Breakpoints not hitting

**Issue**: Breakpoints show as hollow circles (not filled)

**Solution**:
1. Ensure you're building in **Debug** mode (check `Dockerfile.debug`)
2. Verify source mapping in `.vscode/launch.json`: `"/src": "${workspaceRoot}"`
3. Rebuild the container completely:
   ```bash
   docker-compose down
   docker-compose -f docker-compose.yml -f docker-compose.debug.yml up -d --build notification-api
   ```

### Source code doesn't match

**Issue**: Debugger shows "Source code is different from compiled code"

**Solution**:
1. The container source is stale - rebuild:
   ```bash
   docker-compose build notification-api --no-cache
   ```

2. Or use volume mounting (already in `docker-compose.debug.yml`) to sync source

### Local debugging can't connect to Kafka

**Issue**: "Connection refused to localhost:9092"

**Solution**:
The local app is trying to connect to Kafka in Docker. The `.vscode/launch.json` is already configured with `Kafka__BootstrapServers=localhost:9092`, which should work since Kafka exposes port 9092 to localhost.

Verify Kafka is accessible:
```bash
docker-compose ps kafka
telnet localhost 9092
```

---

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

# View all Kafka topics
docker exec -it containerapp-kafka-1 kafka-topics --list --bootstrap-server kafka:9093

# Consume messages from topic (see what's there)
docker exec -it containerapp-kafka-1 kafka-console-consumer --bootstrap-server kafka:9093 --topic weather-alerts --from-beginning --max-messages 5
```

---

## Recommended Workflow

For typical development/debugging sessions:

1. **Start with remote debugging** (Option 1) to debug in the actual container environment
2. **Use local debugging** (Option 2) when you need faster iteration during development
3. **Add strategic log statements** (Option 3) for production-like debugging

**Pro tip**: Keep two terminal windows open:
- Terminal 1: Docker commands and log viewing
- Terminal 2: Kafka message sending for testing
