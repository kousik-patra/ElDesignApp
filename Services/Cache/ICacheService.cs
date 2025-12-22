namespace ElDesignApp.Services.Cache;


using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Distributed;


public interface ICacheService
{
    Task<T?> GetRecordAsync<T>(string recordKey);
    Task SetRecordAsync<T>(string recordKey, T data, TimeSpan? absoluteExpirationRelativeToNow = null);
    // You might also add methods that check cache health if your Cache object allows it
        
    bool IsRedisBased { get; }

    // Methods for clearing cache
        
    // Single key removal
    Task RemoveAsync(string recordKey); 

    // NEW: Method for pattern-based cache removal
    Task RemoveByPatternAsync(string pattern);

    Task FlushDatabaseAsync();
    
}

public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly IConnectionMultiplexer? _redis;
    public bool IsRedisBased { get; }
    
    public CacheService(IDistributedCache cache, IConnectionMultiplexer? redis = null)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _redis = redis;
        IsRedisBased = _redis != null; // True if Redis is active
    }

    public async Task<T?> GetRecordAsync<T>(string recordKey)
        {
            if (!IsRedisBased) return default;

            try
            {
                var json = await _cache.GetStringAsync(recordKey);
                return json == null ? default : JsonSerializer.Deserialize<T>(json);
            }
            catch (RedisConnectionException ex)
            {
                System.Diagnostics.Debug.WriteLine($"{DateTime.Now:hh.mm.ss.ffffff} : Redis GET failed (connection): {ex.Message}. Skipping cache.");
            }
            catch (RedisTimeoutException ex)
            {
                System.Diagnostics.Debug.WriteLine($"{DateTime.Now:hh.mm.ss.ffffff} : Redis GET timeout: {ex.Message}. Skipping cache.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"{DateTime.Now:hh.mm.ss.ffffff} : Redis GET error: {ex.Message}");
            }

            return default;
        }

        public async Task SetRecordAsync<T>(string recordKey, T data, TimeSpan? absoluteExpirationRelativeToNow = null)
        {
            if (!IsRedisBased) return;

            try
            {
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow ?? TimeSpan.FromHours(1)
                };

                var jsonData = JsonSerializer.Serialize(data);
                await _cache.SetStringAsync(recordKey, jsonData, options);
            }
            catch (RedisConnectionException ex)
            {
                System.Diagnostics.Debug.WriteLine($"{DateTime.Now:hh.mm.ss.ffffff} : Redis SET failed (connection): {ex.Message}. Cache write skipped.");
            }
            catch (RedisTimeoutException ex)
            {
                System.Diagnostics.Debug.WriteLine($"{DateTime.Now:hh.mm.ss.ffffff} : Redis SET timeout: {ex.Message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"{DateTime.Now:hh.mm.ss.ffffff} : Redis SET error: {ex.Message}");
            }
            // Never throw — cache write is best-effort
        }

        public async Task FlushDatabaseAsync()
        {
            if (!IsRedisBased || _redis == null)
            {
                System.Diagnostics.Debug.WriteLine($"{DateTime.Now:hh.mm.ss.ffffff} : Flush skipped: Redis not active.");
                return;
            }

            try
            {
                var endpoints = _redis.GetEndPoints(configuredOnly: true);
                if (!endpoints.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"{DateTime.Now:hh.mm.ss.ffffff} : No Redis endpoints found.");
                    return;
                }

                var server = _redis.GetServer(endpoints.First());
                await server.FlushDatabaseAsync();
                System.Diagnostics.Debug.WriteLine($"{DateTime.Now:hh.mm.ss.ffffff} : Redis cache flushed successfully.");
            }
            catch (RedisConnectionException ex)
            {
                System.Diagnostics.Debug.WriteLine($"{DateTime.Now:hh.mm.ss.ffffff} : Redis flush failed (connection down): {ex.Message}");
            }
            catch (RedisTimeoutException ex)
            {
                System.Diagnostics.Debug.WriteLine($"{DateTime.Now:hh.mm.ss.ffffff} : Redis flush timeout: {ex.Message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"{DateTime.Now:hh.mm.ss.ffffff} : Redis flush error: {ex.Message}");
            }
        }

        public async Task RemoveAsync(string recordKey)
        {
            if (!IsRedisBased) return;

            try
            {
                await _cache.RemoveAsync(recordKey);
            }
            catch (RedisConnectionException ex)
            {
                System.Diagnostics.Debug.WriteLine($"{DateTime.Now:hh.mm.ss.ffffff} : Redis REMOVE failed: {ex.Message}");
            }
            catch (RedisTimeoutException)
            {
                // Ignore timeout
            }
        }

        public async Task RemoveByPatternAsync(string pattern)
        {
            if (!IsRedisBased || _redis == null)
            {
                System.Diagnostics.Debug.WriteLine($"{DateTime.Now:hh.mm.ss.ffffff} : Pattern remove skipped: Redis not active.");
                return;
            }

            try
            {
                var endpoints = _redis.GetEndPoints(configuredOnly: true);
                if (!endpoints.Any()) return;

                var server = _redis.GetServer(endpoints.First());
                var db = _redis.GetDatabase();
                var keys = new List<RedisKey>();

                await foreach (var key in server.KeysAsync(pattern: pattern, pageSize: 250))
                {
                    keys.Add(key);
                }

                if (keys.Count > 0)
                {
                    await db.KeyDeleteAsync(keys.ToArray());
                    System.Diagnostics.Debug.WriteLine($"{DateTime.Now:hh.mm.ss.ffffff} : Removed {keys.Count} keys matching '{pattern}'.");
                }
            }
            catch (RedisConnectionException ex)
            {
                System.Diagnostics.Debug.WriteLine($"{DateTime.Now:hh.mm.ss.ffffff} : Pattern remove failed (connection): {ex.Message}");
            }
            catch (RedisTimeoutException)
            {
                System.Diagnostics.Debug.WriteLine($"{DateTime.Now:hh.mm.ss.ffffff} : Pattern remove timeout.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"{DateTime.Now:hh.mm.ss.ffffff} : Pattern remove error: {ex.Message}");
            }
        }
    
    
    
}