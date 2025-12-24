namespace ElDesignApp.Services;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ElDesignApp.Services.Cache;
using ElDesignApp.Services.Global;
using ElDesignApp.Services.DataBase;
using Microsoft.Extensions.Logging;

public interface IDataRetrievalService
{
    Task<(List<T>, string, string, string)> ReadFromCacheOrDb<T>(T item, bool forceDb = false);
    Task<(List<T>, string, string, string)> RefreshCacheAndReadFromDb<T>(T item);
    Task<(string, string, string)> RefreshCache();
    Task InvalidateCacheAsync(string tableName);
}

public class DataRetrievalService : IDataRetrievalService
{
    private readonly Cache.ICacheService _cache;
    //private readonly IMyTableService _myTable;
    private readonly ITableService _myTable;
    private readonly Global.IGlobalDataService _globalData;
    //private readonly IGlobalDataService _globalData;
    private readonly ILogger<DataRetrievalService> _logger;

    public DataRetrievalService(Cache.ICacheService cache, ITableService myTable, 
        Global.IGlobalDataService globalData, ILogger<DataRetrievalService> logger)
    {
        _cache = cache;
        _myTable = myTable;
        _globalData = globalData;
        _logger = logger;
    }
    
    
    public async Task<(List<T>, string, string, string)> ReadFromCacheOrDb<T>(
    T item,
    bool forceDb = false)
{
    string loadLocation = "";
    string logInfo = "";
    string logWarning = "";
    string logError = "";
    var dbName = typeof(T).Name;
    var recordKey = "NavMenu_" + "_" + _globalData.SelectedProject?.Tag + "_" + dbName + "_" + DateTime.Now.ToString("yyyyMMdd_HH");
    List<T> listT = [];
    Stopwatch stopwatch = Stopwatch.StartNew();

    bool redisActive = _cache.IsRedisBased;
    logInfo = $"{DateTime.Now:hh.mm.ss.ffffff} : Starting load for {dbName}. Redis: {redisActive}, ForceDB: {forceDb}";
    _logger.LogInformation(logInfo);

    // Only try cache if Redis is active AND not forcing DB read
    if (!forceDb && redisActive)
    {
        try
        {
            var cached = await _cache.GetRecordAsync<List<T>>(recordKey);
            if (cached != null && cached.Any())
            {
                listT = cached;
                loadLocation = "Redis Cache";
                logInfo = $"{DateTime.Now:hh.mm.ss.ffffff} : Cache HIT → {listT.Count} items from Redis.";
                _logger.LogInformation(logInfo);
            }
        }
        catch (Exception ex)
        {
            logWarning = $"{DateTime.Now:hh.mm.ss.ffffff} : Cache read failed: {ex.Message}. Falling back to DB.";
            _logger.LogWarning(logWarning);
            // Don't rethrow — we want to continue to DB
        }
    }

    // If no cache data (or forced DB), load from database
    if (!listT.Any())
    {
        try
        {
            string selectedProjectTag = _globalData?.SelectedProject?.Tag ?? string.Empty;
            listT = await _myTable.GetListAsync(item, selectedProjectTag) ?? [];

            loadLocation = listT.Any() ? "SQL Database" : "SQL Database (empty)";
            
            if (!listT.Any())
            {
                _globalData.dbConnected = false;
                logWarning = $"Table '{dbName}' returned no data or DB is unreachable.";
                _logger.LogWarning(logWarning);
            }
            else
            {
                logInfo = $"{DateTime.Now:hh.mm.ss.ffffff} : Loaded {listT.Count} items from database.";
                _logger.LogInformation(logInfo);
            }
        }
        catch (Exception ex)
        {
            logError = $"{DateTime.Now:hh.mm.ss.ffffff} : DATABASE ERROR → {ex.Message}";
            _logger.LogError(logError, ex);
            listT = [];
        }

        // Write fresh data to Redis (fire-and-forget, the best effort)

            if (listT.Any() && redisActive)
            {
                try
                {
                    await _cache.SetRecordAsync(recordKey, listT, TimeSpan.FromMinutes(10));
                    logInfo = $"{DateTime.Now:hh.mm.ss.ffffff} : Cache updated for {recordKey}";
                    _logger.LogInformation(logInfo);
                    System.Diagnostics.Debug.WriteLine(logInfo);
                }
                catch (Exception ex)
                {
                    logError = $"{DateTime.Now:hh.mm.ss.ffffff} : Failed to write to Redis: {ex.Message}";
                    System.Diagnostics.Debug.WriteLine(logError);
                    _logger.LogError(logError, ex);
                }
        }
    }

    // Final stats
    var kiloByteSize = listT.Any()
        ? (int)((decimal)Encoding.Unicode.GetByteCount(JsonSerializer.Serialize(listT)) / 1024)
        : 0;

    logInfo = $"Data loaded: {dbName} | Count: {listT.Count} | Size: {kiloByteSize}kB | " +
              $"Time: {stopwatch.ElapsedMilliseconds}ms | Source: '{loadLocation}'";

    _logger.LogInformation(logInfo);
    stopwatch.Stop();

    // Apply sequencing
    listT = _myTable.AssignSequenceToList(listT);

    return (listT, logInfo, logWarning, logError);
}

public async Task<(List<T>, string, string, string)> RefreshCacheAndReadFromDb<T>(T item)
{
    var dbName = typeof(T).Name;
    var recordKey = "NavMenu_"  + "_" + _globalData.SelectedProject.Tag + "_" + dbName + "_" + DateTime.Now.ToString("yyyyMMdd_HH");
    var sw = Stopwatch.StartNew();

    string logInfo = $"{DateTime.Now:hh.mm.ss.ffffff} : FORCED REFRESH STARTED for {dbName}";
    string logWarning = "";
    string logError = "";
    _logger.LogInformation(logInfo);

    bool redisWasActive = _cache.IsRedisBased;

    // Step 1: Try to clear Redis (safe — won't hang)
    if (redisWasActive)
    {
        try
        {
            await _cache.FlushDatabaseAsync();
            logInfo += " | Redis cache cleared.";
        }
        catch (Exception ex)
        {
            logWarning += $" | Redis flush failed: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"{DateTime.Now:hh.mm.ss.ffffff} : Redis unavailable during refresh: {ex.Message}");
        }
    }
    else
    {
        logInfo += " | Redis not active.";
    }

    // Step 2: Force fresh read from DB (bypasses cache completely)
    var (freshData, readInfo, readWarn, readErr) = await ReadFromCacheOrDb(item, forceDb: true);

    // Step 3: Repopulate Redis cache
    if (freshData.Any() && redisWasActive)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await _cache.SetRecordAsync(recordKey, freshData, TimeSpan.FromMinutes(10));
                System.Diagnostics.Debug.WriteLine($"{DateTime.Now:hh.mm.ss.ffffff} : Cache repopulated after refresh.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"{DateTime.Now:hh.mm.ss.ffffff} : Failed to repopulate Redis: {ex.Message}");
            }
        });
        logInfo += " | Cache repopulating in background.";
    }

    sw.Stop();
    logInfo += $" | Total refresh time: {sw.ElapsedMilliseconds}ms";
    _logger.LogInformation(logInfo);

    if (!string.IsNullOrEmpty(readWarn)) logWarning += " | " + readWarn;
    if (!string.IsNullOrEmpty(readErr)) logError += " | " + readErr;

    return (freshData, logInfo, logWarning, logError);
}

public async Task InvalidateCacheAsync(string tableName)
{
    try
    {
        //TODO: Redis implementation in VM docker for development and add-docker in production
        //if (_useRedis && _cache != null)
        if (_cache != null)
        {
            var cacheKey = $"{tableName}_cache";
            await _cache.RemoveAsync(cacheKey);
            _logger.LogInformation($"Cache invalidated for {tableName}");
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Error invalidating cache for {tableName}");
    }
}


public async Task<(string, string, string)> RefreshCache()
{
    var sw = Stopwatch.StartNew();
    string logInfo = $"{DateTime.Now:hh.mm.ss.ffffff} : GLOBAL CACHE REFRESH STARTED";
    string logWarning = "";
    string logError = "";

    if (_cache.IsRedisBased)
    {
        try
        {
            await _cache.FlushDatabaseAsync();
            logInfo += " → Redis cache cleared successfully.";
        }
        catch (Exception ex)
        {
            logWarning = $"Redis unavailable: {ex.Message}";
            logInfo += " → Redis unavailable, skip flush.";
            System.Diagnostics.Debug.WriteLine($"{DateTime.Now:hh.mm.ss.ffffff} : Redis down during global refresh: {ex.Message}");
        }
    }
    else
    {
        logInfo += " → Redis not configured.";
    }

    sw.Stop();
    logInfo += $" | Completed in {sw.ElapsedMilliseconds}ms";
    _logger.LogInformation(logInfo);

    return (logInfo, logWarning, logError);
}
    
    
}