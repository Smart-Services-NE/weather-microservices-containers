namespace WeatherService.Contracts;

public interface ICacheUtility
{
    Task<T?> GetAsync<T>(string key) where T : class;
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
}
