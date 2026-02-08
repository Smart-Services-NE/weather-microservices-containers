# Azure Storage with Azurite

Local Azure Storage emulation using Azurite for development.

## Overview

Azurite provides local emulation for:
- **Blob Storage** - File storage
- **Queue Storage** - Message queues
- **Table Storage** - NoSQL key-value store

Works natively on Apple Silicon (ARM64).

## What's Running

Azurite exposes three services:
- **Blob Storage**: `http://localhost:10000`
- **Queue Storage**: `http://localhost:10001`
- **Table Storage**: `http://localhost:10002`

## Connection Details

### Development Account

Azurite uses a well-known development account:
- **Account Name**: `devstoreaccount1`
- **Account Key**: `Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==`

### Connection Strings

**From Docker containers**:
```
DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://azurite:10000/devstoreaccount1;QueueEndpoint=http://azurite:10001/devstoreaccount1;TableEndpoint=http://azurite:10002/devstoreaccount1;
```

**From host machine (localhost)**:
```
DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://localhost:10000/devstoreaccount1;QueueEndpoint=http://localhost:10001/devstoreaccount1;TableEndpoint=http://localhost:10002/devstoreaccount1;
```

## Usage Examples

### Azure Table Storage (Recommended for Notifications)

Perfect for NoSQL key-value storage of notification records.

**NuGet Package**:
```bash
dotnet add package Azure.Data.Tables
```

**Example Implementation**:
```csharp
using Azure.Data.Tables;

public class NotificationTableAccessor
{
    private readonly TableClient _tableClient;

    public NotificationTableAccessor(IConfiguration configuration)
    {
        var connectionString = configuration["AZURE_STORAGE_CONNECTION_STRING"];
        var serviceClient = new TableServiceClient(connectionString);
        _tableClient = serviceClient.GetTableClient("NotificationRecords");
        _tableClient.CreateIfNotExists();
    }

    public async Task<bool> SaveNotificationAsync(NotificationRecord record)
    {
        var entity = new TableEntity(record.PartitionKey, record.RowKey)
        {
            { "MessageId", record.MessageId },
            { "Subject", record.Subject },
            { "Recipient", record.Recipient },
            { "Status", record.Status }
        };

        await _tableClient.AddEntityAsync(entity);
        return true;
    }

    public async Task<List<NotificationRecord>> GetAllAsync()
    {
        var notifications = new List<NotificationRecord>();
        await foreach (var entity in _tableClient.QueryAsync<TableEntity>())
        {
            notifications.Add(MapToRecord(entity));
        }
        return notifications;
    }
}
```

### Azure Blob Storage

Use for storing large notification payloads or attachments.

**NuGet Package**:
```bash
dotnet add package Azure.Storage.Blobs
```

**Example**:
```csharp
using Azure.Storage.Blobs;

var connectionString = configuration["AZURE_STORAGE_CONNECTION_STRING"];
var blobServiceClient = new BlobServiceClient(connectionString);
var containerClient = blobServiceClient.GetBlobContainerClient("notifications");
await containerClient.CreateIfNotExistsAsync();

// Upload
var blobClient = containerClient.GetBlobClient($"{messageId}.json");
await blobClient.UploadAsync(new BinaryData(notificationJson));
```

### Azure Queue Storage

Use for asynchronous notification processing.

**NuGet Package**:
```bash
dotnet add package Azure.Storage.Queues
```

**Example**:
```csharp
using Azure.Storage.Queues;

var connectionString = configuration["AZURE_STORAGE_CONNECTION_STRING"];
var queueClient = new QueueClient(connectionString, "notification-queue");
await queueClient.CreateIfNotExistsAsync();

// Send message
await queueClient.SendMessageAsync(messageContent);

// Receive messages
var messages = await queueClient.ReceiveMessagesAsync(maxMessages: 10);
```

## Docker Commands

```bash
# Start Azurite
podman compose up -d azurite

# View logs
podman compose logs -f azurite

# Check status
podman ps --filter "name=azurite"

# Stop Azurite
podman compose stop azurite

# Remove Azurite and data
podman compose down -v
```

## Exploring Data

### Azure Storage Explorer

1. Download: https://azure.microsoft.com/features/storage-explorer/
2. Connect to **Local Emulator**
3. Use connection string from above

### REST API

```bash
# List tables
curl -X GET "http://localhost:10002/devstoreaccount1/Tables" \
  -H "Accept: application/json"

# List blobs in container
curl -X GET "http://localhost:10000/devstoreaccount1/notifications?restype=container&comp=list"
```

## Production Migration

When deploying to Azure:

1. Create Azure Storage Account in Azure Portal
2. Get connection string from Portal
3. Update environment variables:
```bash
AZURE_STORAGE_CONNECTION_STRING=<your-azure-connection-string>
AZURE_TABLE_NAME=NotificationRecords
```
4. No code changes needed - same SDK works for both

## Benefits of Azurite

- **ARM64 Compatible** - Works natively on Apple Silicon
- **Fast Startup** - Starts in seconds
- **Lightweight** - Minimal resource usage
- **Full Feature Support** - Same APIs as Azure Storage
- **Data Persistence** - Data stored in Docker volume
- **No Azure Account Required** - Perfect for local development

## Partition Strategy for Table Storage

**Good partition key choices for notifications**:

```csharp
// Distribute by recipient
PartitionKey = recipient.GetHashCode().ToString("X");
RowKey = $"{DateTime.UtcNow.Ticks}_{messageId}";

// Or partition by date
PartitionKey = DateTime.UtcNow.ToString("yyyy-MM-dd");
RowKey = messageId;
```

## Switching from SQLite to Table Storage

**Benefits**:
- Better scalability for production
- NoSQL flexibility
- Automatic indexing by PartitionKey and RowKey
- Built-in redundancy in Azure (when deployed)
- Seamless local-to-cloud transition

**Implementation Steps**:
1. Install `Azure.Data.Tables` package
2. Create `NotificationTableAccessor` following IDesign architecture
3. Update `NotificationManager` to use new accessor
4. Test locally with Azurite
5. Deploy to Azure with real connection string

## Related Documentation

- [Project Architecture](../../CLAUDE.md)
- [NotificationService Monitoring](../services/notification-service.md)
- [Quick Start Guide](../getting-started/quick-start.md)
