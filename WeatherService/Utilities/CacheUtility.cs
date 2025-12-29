using Microsoft.Extensions.Caching.Hybrid;
using WeatherService.Contracts;

namespace WeatherService.Utilities;

public class CacheUtility : ICacheUtility
{
    private readonly HybridCache _cache;

    public CacheUtility(HybridCache cache)
    {
        _cache = cache;
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            return await _cache.GetOrCreateAsync<T?>(
                key,
                cancel => ValueTask.FromResult<T?>(null)
            );
        }
        catch
        {
            return null;
        }
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        var options = new HybridCacheEntryOptions
        {
            Expiration = expiration ?? TimeSpan.FromMinutes(5)
        };

        return await _cache.GetOrCreateAsync(
            key,
            async cancel => await factory(),
            options
        );
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        var options = new HybridCacheEntryOptions
        {
            Expiration = expiration ?? TimeSpan.FromMinutes(5)
        };

        await _cache.SetAsync(key, value, options);
    }

    public async Task RemoveAsync(string key)
    {
        await _cache.RemoveAsync(key);
    }
}
