using Confluent.Kafka;
using Confluent.SchemaRegistry.Serdes;

namespace NotificationService.Utilities;

/// <summary>
/// Adapter that wraps an async deserializer to make it compatible with synchronous Kafka consumer.
/// This is equivalent to the .AsSyncOverAsync() extension method from older Confluent.Kafka versions.
/// </summary>
public class SyncOverAsyncDeserializer<T> : IDeserializer<T>
{
    private readonly IAsyncDeserializer<T> _asyncDeserializer;

    public SyncOverAsyncDeserializer(IAsyncDeserializer<T> asyncDeserializer)
    {
        _asyncDeserializer = asyncDeserializer;
    }

    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        // Convert ReadOnlySpan to byte array for async deserializer
        var byteArray = data.ToArray();

        // Block on the async operation (this is what .AsSyncOverAsync() did)
        return _asyncDeserializer.DeserializeAsync(byteArray, isNull, context)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }
}
