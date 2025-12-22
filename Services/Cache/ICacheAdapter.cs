using System;
using System.Threading.Tasks;

namespace ElDesignApp.Services.Cache
{
    public interface ICacheAdapter
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? ttl = null);
        Task RemoveAsync(string key);
        Task RemoveByPatternAsync(string pattern);
        Task FlushDatabaseAsync();
        bool IsRedisBased { get; }
    }

    public class CacheAdapter : ICacheAdapter
    {
        private readonly ICacheService _cacheService;

        public CacheAdapter(ICacheService cacheService)
        {
            _cacheService = cacheService 
                            ?? throw new ArgumentNullException(nameof(cacheService));
        }

        public bool IsRedisBased => _cacheService.IsRedisBased;

        public async Task<T?> GetAsync<T>(string key)
        {
            return await _cacheService.GetRecordAsync<T>(key);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null)
        {
            await _cacheService.SetRecordAsync<T>(key, value, ttl);
        }

        public async Task RemoveAsync(string key)
        {
            await _cacheService.RemoveAsync(key);
        }

        public async Task RemoveByPatternAsync(string pattern)
        {
            await _cacheService.RemoveByPatternAsync(pattern);
        }

        public async Task FlushDatabaseAsync()
        {
            await _cacheService.FlushDatabaseAsync();
        }
    }
}