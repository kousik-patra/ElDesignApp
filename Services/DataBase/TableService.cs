using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Dapper;
using ElDesignApp.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;


namespace ElDesignApp.Services.DataBase;

#region Custom Exceptions

/// <summary>
/// Base exception for all table service operations
/// </summary>
public class TableServiceException : Exception
{
    public string? TableName { get; }
    public string? Operation { get; }

    public TableServiceException(string message, string? tableName = null, string? operation = null, Exception? inner = null)
        : base(message, inner)
    {
        TableName = tableName;
        Operation = operation;
    }
}

/// <summary>
/// Thrown when a table does not exist in the database
/// </summary>
public class TableNotFoundException : TableServiceException
{
    public TableNotFoundException(string tableName)
        : base($"Table '{tableName}' does not exist in the database.", tableName, "Query") { }
}

/// <summary>
/// Thrown when a record already exists (duplicate key)
/// </summary>
public class DuplicateRecordException : TableServiceException
{
    public DuplicateRecordException(string tableName, string message, Exception? inner = null)
        : base(message, tableName, "Insert", inner) { }
}

/// <summary>
/// Thrown when a required record is not found
/// </summary>
public class RecordNotFoundException : TableServiceException
{
    public RecordNotFoundException(string tableName, string message)
        : base(message, tableName, "Query") { }
}

/// <summary>
/// Thrown when validation fails
/// </summary>
public class ValidationException : TableServiceException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(string message, IDictionary<string, string[]>? errors = null)
        : base(message)
    {
        Errors = errors ?? new Dictionary<string, string[]>();
    }
}

#endregion

#region Result Types

/// <summary>
/// Result of bulk update operations
/// </summary>
public record UpdateResult(int Added, int Modified, int Deleted, List<string> Errors)
{
    public bool HasErrors => Errors.Count > 0;
    public int TotalAffected => Added + Modified + Deleted;
}

/// <summary>
/// Result of Excel import operations
/// </summary>
public record ImportResult<T>(List<T> Items, int SuccessCount, int FailedCount, List<string> Errors)
{
    public bool HasErrors => FailedCount > 0;
    public string Summary => $"Imported: {SuccessCount}, Failed: {FailedCount}" +
                             (HasErrors ? $". Errors: {string.Join("; ", Errors.Take(5))}" : "");
}

#endregion

#region Interface

/// <summary>
/// Unified table service for all database operations
/// </summary>
public interface ITableService
{
    #region Core CRUD Operations

    /// <summary>
    /// Check if a table exists in the database
    /// </summary>
    Task<bool> TableExistsAsync(string tableName);
    
    /// <summary>
    /// Get all records for a table, optionally filtered by project
    /// </summary>
    Task<List<T>> GetListAsync<T>(string? projectId = null) where T : class, new();

    /// <summary>
    /// Insert a single item
    /// </summary>
    Task<int> InsertAsync<T>(T item) where T : class;
    
    /// <summary>
    /// UpdateParentCallback all fields of an item (identified by UID or unique fields)
    /// </summary>
    Task<int> UpdateAsync<T>(T item) where T : class?;

    /// <summary>
    /// UpdateParentCallback specific fields of an item (auto-detects by UID or unique fields)
    /// </summary>
    Task<int> UpdateFieldsAsync<T>(T item, params string[] fields) where T : class;

    /// <summary>
    /// UpdateParentCallback specific fields by UID
    /// </summary>
    Task<int> UpdateFieldsByUidAsync<T>(T item, Guid uid, IEnumerable<string> fields) where T : class;

    /// <summary>
    /// Delete an item by UID
    /// </summary>
    Task<int> DeleteAsync<T>(Guid uid) where T : class;

    /// <summary>
    /// Delete an item
    /// </summary>
    Task<int> DeleteAsync<T>(T item) where T : class;

    /// <summary>
    /// Check if any record matches the predicate
    /// </summary>
    Task<bool> AnyAsync<T>(Expression<Func<T, bool>> predicate) where T : class;

    /// <summary>
    /// Check if any record matches the property value
    /// </summary>
    Task<bool> AnyAsync<T>(string propertyName, object value) where T : class;

    #endregion

    #region Bulk Operations

    /// <summary>
    /// Synchronize a list with the database (add/update/delete as needed)
    /// </summary>
    Task<UpdateResult> SyncListAsync<T>(List<T> currentList, List<T> originalList, string? updatedBy = null) where T : class;

    /// <summary>
    /// Bulk copy data to a table (replaces existing project data)
    /// </summary>
    Task BulkCopyAsync<T>(List<T> items) where T : class;

    #endregion

    #region Raw SQL Operations
    
    /// <summary>
    /// Execute a query and return results
    /// </summary>
    Task<List<T>> QueryAsync<T>(string sql, object? parameters = null);

    /// <summary>
    /// Execute a command (INSERT, UPDATE, DELETE)
    /// </summary>
    Task<int> ExecuteAsync(string sql, object? parameters = null, int? timeoutSeconds = null);

    #endregion

    #region Schema Operations

    /// <summary>
    /// Get column names for a table
    /// </summary>
    Task<List<string>> GetColumnNamesAsync(string tableName);

    /// <summary>
    /// Get all table names in the database
    /// </summary>
    Task<List<string>> GetTableNamesAsync();
    
    /// <summary>
    /// Get all table names from a specific database connection
    /// </summary>
    Task<List<string>> GetTableNamesAsync(string connectionString);


    #endregion

    #region Excel Operations
    
    /// <summary>
    /// Async version - Exports items to Excel with SQL column filtering
    /// </summary>
    Task<byte[]> ExportExcelTemplateAsync<T>() where T : new();
    
    
    /// <summary>
    /// Import data from Excel file
    /// </summary>
    Task<ImportResult<T>> ImportFromExcelAsync<T>(IBrowserFile file, string? sheetName = null) where T : class, new();

    /// <summary>
    /// Import data from Excel file (InputFileChangeEventArgs overload)
    /// </summary>
    Task<ImportResult<T>> ImportFromExcelAsync<T>(InputFileChangeEventArgs e, string? sheetName = null) where T : class, new();

    /// <summary>
    /// Generate Excel workbook bytes from a list
    /// </summary>
    byte[] ExportToExcel<T>(List<T>? items) where T : new();

    #endregion

    #region Utility Methods

    /// <summary>
    /// Assign sequence numbers to items missing them
    /// </summary>
    List<T> AssignSequence<T>(List<T> items) where T : class;

    #endregion

    #region Database Copy Operations

    /// <summary>
    /// Copy tables between databases
    /// </summary>
    Task CopyTablesAsync(string sourceConnectionString, string destConnectionString, IEnumerable<string> tableNames);

    #endregion
}

#endregion

#region Implementation

public class TableService : ITableService
{
    #region Constants & Fields

    private const string UID_FIELD = "UID";
    private const string PROJECT_ID_FIELD = "ProjectId";
    private const string UPDATED_BY_FIELD = "UpdatedBy";
    private const string UPDATED_ON_FIELD = "UpdatedOn";
    private const int DEFAULT_TIMEOUT_SECONDS = 30;

    private static readonly string[] UniqueFieldCandidates =
        { "Tag", "Code", "Name", "Email", "UserName", "ProjectCode", "ProjectTag", "LoginId" };

    private static readonly string[] ProjectExcludedTables =
        { "RoleMapping", "DBMaster", "Project", "ProjectUserAssignment", "ProjectUserRole" };

    private static readonly string[] DateTimeFormats =
    {
        "yyyy-MM-ddTHH:mm:ss.fff", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd",
        "MM/dd/yyyy HH:mm:ss", "MM/dd/yyyy", "dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy",
        "yyyy/MM/dd HH:mm:ss", "yyyy/MM/dd"
    };
    
    private static readonly string[] DefaultImportExcludedFields =
        { "UID", "UpdatedBy", "UpdatedOn", "CellCSS", "Save2DB" };

    private readonly IConfiguration _configuration;
    private readonly ILogger<TableService> _logger;
    private readonly string _connectionString;
    private readonly IUserContextService _userContext;

    // Column name cache to avoid repeated schema queries
    private readonly Dictionary<string, List<string>> _columnCache = new();
    private readonly object _cacheLock = new();
    private readonly string _userName;

    #endregion

    #region Constructor

    public TableService(
        IConfiguration configuration, 
        ILogger<TableService> logger, 
        IUserContextService userContext)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _connectionString = _configuration.GetConnectionString("DefaultConnection")
                            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));

    }

    #endregion

    #region Connection Management

    private SqlConnection CreateConnection() => new(_connectionString);

    private async Task<T> WithConnectionAsync<T>(Func<SqlConnection, Task<T>> operation)
    {
        await using var connection = CreateConnection();
        try
        {
            await connection.OpenAsync();
            return await operation(connection);
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            _logger.LogWarning(ex, "Duplicate key violation");
            throw new DuplicateRecordException("Unknown", "A record with this key already exists.", ex);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error occurred. Number: {Number}", ex.Number);
            throw new TableServiceException($"Database error: {ex.Message}", operation: "Query", inner: ex);
        }
        catch (Exception ex) when (ex is not TableServiceException)
        {
            _logger.LogError(ex, "Unexpected error in database operation");
            throw new TableServiceException($"Unexpected error: {ex.Message}", inner: ex);
        }
    }

    private async Task WithConnectionAsync(Func<SqlConnection, Task> operation)
    {
        await WithConnectionAsync(async conn =>
        {
            await operation(conn);
            return 0;
        });
    }

    #endregion

    #region Core CRUD Operations

    /// <summary>
    /// Check if a table exists in the database
    /// </summary>
    public async Task<bool> TableExistsAsync(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            return false;

        // Check cache first - if we have columns cached, table exists
        lock (_cacheLock)
        {
            if (_columnCache.ContainsKey(tableName))
                return true;
        }

        var sql = @"SELECT CASE WHEN EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @TableName
                ) THEN 1 ELSE 0 END";

        try
        {
            var result = await WithConnectionAsync(async conn =>
                await conn.ExecuteScalarAsync<int>(sql, new { TableName = tableName }));

            return result == 1;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking if table '{TableName}' exists", tableName);
            return false;
        }
    }
    
    
    public async Task<List<T>> GetListAsync<T>(string? projectId = null) where T : class, new()
    {
        var tableName = typeof(T).Name;
        
        var stopwatch = Stopwatch.StartNew();
        var actualTableName = tableName;
        
        // Special case for LoadMaster -> MasterLoadList mapping
        if (tableName == "LoadMaster")
        {
            actualTableName = "MasterLoadList";
        }
        
        
        // Check if table exists first
        var tableExists = await TableExistsAsync(actualTableName);
        if (!tableExists)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "Table '{TableName}' does not exist in database. Returning empty list. " +
                "Consider creating the table or removing the call to GetListAsync<{TypeName}>().",
                actualTableName, tableName);
        
            return new List<T>();
        }
        

        // Build SQL with optional project filter
        var sql = $"SELECT * FROM dbo.[{tableName}]";

        var needsProjectFilter = !string.IsNullOrEmpty(projectId)
                                 && !tableName.Contains("Data")
                                 && !ProjectExcludedTables.Contains(tableName);

        if (needsProjectFilter)
        {
            sql += $" WHERE [{PROJECT_ID_FIELD}] = @ProjectId";
        }


        try
        {
            var result = await QueryAsync<T>(sql, new { ProjectId = projectId ?? string.Empty });

            stopwatch.Stop();
            var source = result.Count > 0 ? "SQL Database" : "SQL Database (empty)";
            var sizeKb = EstimateListSizeKb(result);
        
            _logger.LogInformation(
                "Data loaded: {Table} | Count: {Count} | Size: {Size}kB | Time: {Time}ms | Source: '{Source}'",
                tableName, result.Count, sizeKb, stopwatch.ElapsedMilliseconds, source);

            return result;
        }
        catch (SqlException ex) when (ex.Number == 208) // Invalid object name
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "Table '{TableName}' was not found (SQL Error 208). Returning empty list.",
                actualTableName);
        
            return new List<T>();
        }
    }
    
    /// <summary>
    /// Estimate the size of a list in KB (rough approximation)
    /// </summary>
    private static int EstimateListSizeKb<T>(List<T> list)
    {
        if (list == null || list.Count == 0)
            return 0;

        // Rough estimate: serialize first item and multiply
        try
        {
            var sample = System.Text.Json.JsonSerializer.Serialize(list.Take(Math.Min(10, list.Count)));
            var avgItemSize = sample.Length / Math.Min(10, list.Count);
            return (avgItemSize * list.Count) / 1024;
        }
        catch
        {
            return list.Count; // Fallback: assume 1KB per item
        }
    }

    public async Task<int> InsertAsync<T>(T item) where T : class
    {
        if (item == null) throw new ArgumentNullException(nameof(item));

        var tableName = typeof(T).Name;
        var dbColumns = await GetColumnNamesAsync(tableName);

        if (dbColumns.Count == 0)
            throw new TableServiceException($"No columns found for table '{tableName}'", tableName, "Insert");

        // Get properties that match DB columns
        var properties = typeof(T).GetProperties()
            .Where(p => dbColumns.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (properties.Count == 0)
            throw new TableServiceException($"No matching properties found for table '{tableName}'", tableName, "Insert");

        var columns = properties.Select(p => $"[{p.Name}]");
        var parameters = properties.Select(p => $"@{p.Name}");

        var sql = $"INSERT INTO dbo.[{tableName}] ({string.Join(", ", columns)}) VALUES ({string.Join(", ", parameters)})";

        var dynamicParams = new DynamicParameters();
        foreach (var prop in properties)
        {
            dynamicParams.Add($"@{prop.Name}", prop.GetValue(item));
        }

        _logger.LogDebug("Inserting into {Table}: {Sql}", tableName, sql);
        return await ExecuteAsync(sql, dynamicParams);
    }
    
    /// <summary>
/// UpdateParentCallback all fields of an item (identified by UID or unique fields)
/// </summary>
public async Task<int> UpdateAsync<T>(T item) where T : class
{
    if (item == null) throw new ArgumentNullException(nameof(item));

    var tableName = typeof(T).Name;
    var dbColumns = await GetColumnNamesAsync(tableName);

    if (dbColumns.Count == 0)
        throw new TableServiceException($"No columns found for table '{tableName}'", tableName, "UpdateParentCallback");

    // Build WHERE clause
    var (whereClause, whereParams) = BuildWhereClause(item);
    if (string.IsNullOrEmpty(whereClause))
        throw new TableServiceException("Cannot determine record identity for update. Provide UID or unique field.", tableName, "UpdateParentCallback");

    // Get properties that match DB columns (excluding the identifier used in WHERE)
    var identifierFields = whereParams.Keys.Select(k => k.TrimStart('@')).ToHashSet(StringComparer.OrdinalIgnoreCase);
    
    var updateableColumns = dbColumns
        .Where(col => !identifierFields.Contains(col)) // Don't update the identifier
        .ToList();

    if (updateableColumns.Count == 0)
    {
        _logger.LogWarning("No updateable columns found for {Table}", tableName);
        return 0;
    }

    // Build SET clause
    var setClauses = updateableColumns.Select(col => $"[{col}] = @{col}");
    var sql = $"UPDATE dbo.[{tableName}] SET {string.Join(", ", setClauses)} WHERE {whereClause}";

    // Build parameters
    var parameters = new DynamicParameters();
    var type = typeof(T);

    foreach (var col in updateableColumns)
    {
        var prop = type.GetProperty(col, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        parameters.Add($"@{col}", prop?.GetValue(item));
    }

    // Add WHERE clause parameters
    foreach (var (key, value) in whereParams)
    {
        parameters.Add(key, value);
    }

    _logger.LogDebug("UpdateAsync {Table}: {Sql}", tableName, sql);
    return await ExecuteAsync(sql, parameters);
}

    public async Task<int> UpdateFieldsAsync<T>(T item, params string[] fields) where T : class
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        if (fields == null || fields.Length == 0) return 0;

        var tableName = typeof(T).Name;
        var dbColumns = await GetColumnNamesAsync(tableName);

        // Validate fields exist in DB
        var validFields = fields.Where(f => dbColumns.Contains(f, StringComparer.OrdinalIgnoreCase)).ToList();
        if (validFields.Count == 0)
        {
            _logger.LogWarning("No valid fields to update for {Table}. Requested: {Fields}", tableName, string.Join(", ", fields));
            return 0;
        }

        // Build WHERE clause
        var (whereClause, whereParams) = BuildWhereClause(item);
        if (string.IsNullOrEmpty(whereClause))
            throw new TableServiceException("Cannot determine record identity for update. Provide UID or unique field.", tableName, "UpdateParentCallback");

        // Build SET clause
        var setClauses = validFields.Select(f => $"[{f}] = @{f}");
        var sql = $"UPDATE dbo.[{tableName}] SET {string.Join(", ", setClauses)} WHERE {whereClause}";

        var parameters = new DynamicParameters();
        foreach (var field in validFields)
        {
            var prop = typeof(T).GetProperty(field, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            parameters.Add($"@{field}", prop?.GetValue(item));
        }

        foreach (var (key, value) in whereParams)
        {
            parameters.Add(key, value);
        }

        _logger.LogDebug("Updating {Table}: {Sql}", tableName, sql);
        return await ExecuteAsync(sql, parameters);
    }

    public async Task<int> UpdateFieldsByUidAsync<T>(T item, Guid uid, IEnumerable<string> fields) where T : class
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        if (uid == Guid.Empty) throw new ArgumentException("UID cannot be empty", nameof(uid));

        var fieldList = fields.ToList();
        if (fieldList.Count == 0) return 0;

        var tableName = typeof(T).Name;
        var dbColumns = await GetColumnNamesAsync(tableName);

        var validFields = fieldList.Where(f => dbColumns.Contains(f, StringComparer.OrdinalIgnoreCase)).ToList();
        if (validFields.Count == 0) return 0;

        // Add audit fields
        var auditFields = new[] { UPDATED_ON_FIELD, UPDATED_BY_FIELD };
        validFields.AddRange(auditFields.Where(af => dbColumns.Contains(af) && !validFields.Contains(af)));

        var setClauses = validFields.Select(f => $"[{f}] = @{f}");
        var sql = $"UPDATE dbo.[{tableName}] SET {string.Join(", ", setClauses)} WHERE [{UID_FIELD}] = @UID";

        var parameters = new DynamicParameters();
        parameters.Add("@UID", uid);

        foreach (var field in validFields)
        {
            var prop = typeof(T).GetProperty(field, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (prop != null)
            {
                var value = field == UPDATED_ON_FIELD ? DateTime.Now : prop.GetValue(item);
                parameters.Add($"@{field}", value);
            }
        }

        return await ExecuteAsync(sql, parameters);
    }

    public async Task<int> DeleteAsync<T>(Guid uid) where T : class
    {
        if (uid == Guid.Empty) throw new ArgumentException("UID cannot be empty", nameof(uid));

        var tableName = typeof(T).Name;
        var sql = $"DELETE FROM dbo.[{tableName}] WHERE [{UID_FIELD}] = @UID";

        _logger.LogDebug("Deleting from {Table} where UID = {Uid}", tableName, uid);
        return await ExecuteAsync(sql, new { UID = uid });
    }

    public async Task<int> DeleteAsync<T>(T item) where T : class
    {
        if (item == null) throw new ArgumentNullException(nameof(item));

        var uidProp = GetUidProperty<T>();
        if (uidProp == null)
            throw new TableServiceException($"Type {typeof(T).Name} must have a UID property", typeof(T).Name, "Delete");

        var uid = uidProp.GetValue(item) as Guid? ?? Guid.Empty;
        if (uid == Guid.Empty)
            throw new TableServiceException("Cannot delete item with empty UID", typeof(T).Name, "Delete");

        return await DeleteAsync<T>(uid);
    }

    public async Task<bool> AnyAsync<T>(Expression<Func<T, bool>> predicate) where T : class
    {
        var (propertyName, value) = ExtractPropertyAndValue(predicate);
        return await AnyAsync<T>(propertyName, value);
    }

    public async Task<bool> AnyAsync<T>(string propertyName, object value) where T : class
    {
        if (string.IsNullOrWhiteSpace(propertyName))
            throw new ArgumentException("Property name is required", nameof(propertyName));

        var tableName = typeof(T).Name;
        var sql = $"SELECT TOP 1 1 FROM dbo.[{tableName}] WHERE [{propertyName}] = @Value";

        var result = await WithConnectionAsync(async conn =>
            await conn.QueryFirstOrDefaultAsync<int?>(sql, new { Value = value }));

        return result == 1;
    }

    #endregion

    #region Bulk Operations

    public async Task<UpdateResult> SyncListAsync<T>(List<T> currentList, List<T> originalList, string? updatedBy = null) where T : class
    {
        currentList ??= new List<T>();
        originalList ??= new List<T>();
        updatedBy ??= "System";

        var errors = new List<string>();
        int added = 0, modified = 0, deleted = 0;

        var type = typeof(T);
        var tableName = type.Name;

        var uidProp = GetUidProperty<T>();
        if (uidProp == null)
            throw new TableServiceException($"Type {tableName} must have a UID property", tableName, "SyncList");

        var dbColumns = await GetColumnNamesAsync(tableName);

        // Build lookup from original list
        var originalByUid = originalList
            .Where(item => uidProp.GetValue(item) is Guid uid && uid != Guid.Empty)
            .ToDictionary(item => (Guid)uidProp.GetValue(item)!, item => item);

        await using var connection = CreateConnection();
        await connection.OpenAsync();
        await using var transaction = connection.BeginTransaction();

        try
        {
            var processedUids = new HashSet<Guid>();

            foreach (var item in currentList)
            {
                try
                {
                    // Ensure UID exists
                    var uid = EnsureUid(item, uidProp);
                    processedUids.Add(uid);

                    if (originalByUid.TryGetValue(uid, out var originalItem))
                    {
                        // UPDATE - find changed fields
                        var changedFields = FindChangedFields(item, originalItem, dbColumns);

                        if (changedFields.Count > 0)
                        {
                            SetAuditFields(item, updatedBy);

                            var setClauses = changedFields.Select(c => $"[{c}] = @{c}");
                            var sql = $"UPDATE dbo.[{tableName}] SET {string.Join(", ", setClauses)} WHERE [{UID_FIELD}] = @UID";

                            var parameters = BuildParameters(item, changedFields, uidProp);
                            await connection.ExecuteAsync(sql, parameters, transaction);
                            modified++;
                        }
                    }
                    else
                    {
                        // INSERT
                        SetAuditFields(item, updatedBy);

                        var insertableProps = type.GetProperties()
                            .Where(p => dbColumns.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
                            .ToList();

                        var columns = insertableProps.Select(p => $"[{p.Name}]");
                        var paramNames = insertableProps.Select(p => $"@{p.Name}");

                        var sql = $"INSERT INTO dbo.[{tableName}] ({string.Join(", ", columns)}) VALUES ({string.Join(", ", paramNames)})";

                        var parameters = new DynamicParameters();
                        foreach (var prop in insertableProps)
                        {
                            parameters.Add($"@{prop.Name}", prop.GetValue(item));
                        }

                        var insertResult = await connection.ExecuteAsync(sql, parameters, transaction);
                        added++;
                    }
                }
                catch (Exception ex)
                {
                    var uid = uidProp.GetValue(item)?.ToString() ?? "unknown";
                    errors.Add($"Item {uid}: {ex.Message}");
                    _logger.LogWarning(ex, "Failed to process item {Uid} in {Table}", uid, tableName);
                }
            }

            // DELETE removed items
            var toDelete = originalByUid.Keys.Except(processedUids).ToList();
            foreach (var uid in toDelete)
            {
                try
                {
                    var sql = $"DELETE FROM dbo.[{tableName}] WHERE [{UID_FIELD}] = @UID";
                    await connection.ExecuteAsync(sql, new { UID = uid }, transaction);
                    deleted++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Delete {uid}: {ex.Message}");
                    _logger.LogWarning(ex, "Failed to delete item {Uid} from {Table}", uid, tableName);
                }
            }

            await transaction.CommitAsync();
            _logger.LogInformation("SyncList completed for {Table}: Added={Added}, Modified={Modified}, Deleted={Deleted}",
                tableName, added, modified, deleted);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "SyncList failed for {Table}, transaction rolled back", tableName);
            throw new TableServiceException($"Sync failed for {tableName}: {ex.Message}", tableName, "SyncList", ex);
        }

        return new UpdateResult(added, modified, deleted, errors);
    }

    public async Task BulkCopyAsync<T>(List<T> items) where T : class
    {
        var (userName, projectId) = await _userContext.GetContextAsync();
        if (items == null || items.Count == 0) return;
        if (string.IsNullOrWhiteSpace(projectId))
            throw new ArgumentException("Project ID is required", nameof(projectId));

        var tableName = typeof(T).Name;
        
        // Clear cache for this table to ensure fresh column list
        lock (_cacheLock)
        {
            _columnCache.Remove(tableName);
        }


        // Set UpdatedBy for all items
        var updatedByProp = typeof(T).GetProperty(UPDATED_BY_FIELD);
        if (updatedByProp != null)
        {
            foreach (var item in items)
            {
                updatedByProp.SetValue(item, userName);
            }
        }
        
        // Set UpdatedOn for all items
        var updatedOnProp = typeof(T).GetProperty(UPDATED_ON_FIELD);
        if (updatedOnProp != null)
        {
            foreach (var item in items)
            {
                updatedOnProp.SetValue(item, DateTime.Now);
            }
        }

        // Delete existing data for project
        await ExecuteAsync($"DELETE FROM dbo.[{tableName}] WHERE [{PROJECT_ID_FIELD}] = @ProjectId",
            new { ProjectId = projectId });

        // Create DataTable from list
        var dataTable = ToDataTable(items);

        await using var connection = CreateConnection();
        await connection.OpenAsync();

        using var bulkCopy = new SqlBulkCopy(connection)
        {
            DestinationTableName = $"dbo.[{tableName}]",
            BatchSize = 1000,
            BulkCopyTimeout = 120
        };

        foreach (DataColumn column in dataTable.Columns)
        {
            bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
        }

        try
        {
            await bulkCopy.WriteToServerAsync(dataTable);
            _logger.LogInformation("Bulk copied {Count} rows to {Table}", items.Count, tableName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk copy failed for {Table}", tableName);
            throw new TableServiceException($"Bulk copy failed: {ex.Message}", tableName, "BulkCopy", ex);
        }
    }

    #endregion

    #region Raw SQL Operations

    public async Task<List<T>> QueryAsync<T>(string sql, object? parameters = null)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("SQL query is required", nameof(sql));

        return await WithConnectionAsync(async conn =>
        {
            var result = await conn.QueryAsync<T>(sql, parameters);
            return result.ToList();
        });
    }

    public async Task<int> ExecuteAsync(string sql, object? parameters = null, int? timeoutSeconds = null)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("SQL command is required", nameof(sql));

        return await WithConnectionAsync(async conn =>
            await conn.ExecuteAsync(new CommandDefinition(sql, parameters,
                commandTimeout: timeoutSeconds ?? DEFAULT_TIMEOUT_SECONDS)));
    }

    #endregion

    #region Schema Operations

    public async Task<List<string>> GetColumnNamesAsync(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name is required", nameof(tableName));

        // Check cache first
        lock (_cacheLock)
        {
            if (_columnCache.TryGetValue(tableName, out var cached))
                return cached;
        }

        var sql = @"SELECT COLUMN_NAME 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = @TableName 
                    ORDER BY ORDINAL_POSITION";

        var columns = await QueryAsync<string>(sql, new { TableName = tableName });

        // Cache the result
        lock (_cacheLock)
        {
            _columnCache[tableName] = columns;
        }

        return columns;
    }

    public async Task<List<string>> GetTableNamesAsync()
    {
        var sql = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";
        return await QueryAsync<string>(sql);
    }

    public async Task<List<string>> GetTableNamesAsync(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string is required", nameof(connectionString));

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
    
        var sql = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME";
        var tables = await connection.QueryAsync<string>(sql);
        return tables.ToList();
    }
    /// <summary>
    /// Clear the column name cache (useful after schema changes)
    /// </summary>
    public void ClearColumnCache()
    {
        lock (_cacheLock)
        {
            _columnCache.Clear();
        }
        _logger.LogInformation("Column cache cleared");
    }

    #endregion

    #region Excel Operations
    
    
    /// <summary>
    /// Async version - Exports items to Excel with SQL column filtering
    /// </summary>
    public async Task<byte[]> ExportExcelTemplateAsync<T>() where T : new()
    {
        var tableName = typeof(T).Name;

        var sqlColumns = await GetColumnNamesAsync(tableName);
        
        if (sqlColumns.Count == 0)
        {
            throw new InvalidOperationException($"Table '{tableName}' not found or has no columns.");
        }
        
        return await Task.Run(() => GenerateExcelTemplate<T>(sqlColumns));

    }

    private byte[] GenerateExcelTemplate<T>( List<string> sqlColumns)
    {
        
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add(typeof(T).Name);
        
        // Define the exclusion list
        var excludeList = new[] { "UID", "ProjectId", "UpdatedBy", "UpdatedOn"};

        List<PropertyInfo> properties = [];
        try
        {
            properties = typeof(T).GetProperties()
                .Where(p => sqlColumns.Contains(p.Name, StringComparer.OrdinalIgnoreCase)
                            && !excludeList.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
                .OrderBy(p => p.GetCustomAttribute<DisplayAttribute>()?.GetOrder() ?? int.MaxValue)
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        if (properties.Count == 0)
        {
            throw new InvalidOperationException($"No matching columns found between class '{typeof(T).Name}' and SQL table.");
        }

        var dataRowRange = 99999;
        // Headers - Row 1: Property names
        for (int col = 0; col < properties.Count; col++)
        {
            var prop = properties[col];
            var colIndex = col + 1;
            
            // Set Row 1 (Property Name)
            ws.Cells[1, col + 1].Value = properties[col].Name;
            
            // Set Row 2 (Display Name)
            var displayName = prop.GetCustomAttribute<DisplayAttribute>()?.Name ?? prop.Name;
            ws.Cells[2, colIndex].Value = displayName;
            
            // 2. Highlight "Tag" field
            if (prop.Name.Equals("Tag", StringComparison.OrdinalIgnoreCase))
            {
                // 1. Highlight Header (Existing logic)
                var headerCells = ws.Cells[1, colIndex, 2, colIndex];
                headerCells.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                headerCells.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow);

                // 2. Add Tooltip (Comment) to the Display Name header (Row 2)
                var comment = ws.Cells[2, colIndex].AddComment("All tags shall be unique.", "System");
                comment.AutoFit = true;

                // 3. Add Conditional Formatting for Duplicates
                // We apply this to the data range below the headers (Row 3 down to dataRowRange)
                var dataRange = ws.Cells[3, colIndex, dataRowRange, colIndex];
                var duplicateFormatting = ws.ConditionalFormatting.AddDuplicateValues(dataRange);
    
                // Set the style for when a duplicate is found (e.g., light red fill with dark red text)
                duplicateFormatting.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                duplicateFormatting.Style.Fill.BackgroundColor.Color = System.Drawing.Color.FromArgb(255, 199, 206);
                duplicateFormatting.Style.Font.Color.Color = System.Drawing.Color.FromArgb(156, 0, 6);
            }
        }


        // Data shall be empty as it's a template for data input

        // Formatting
        var range = ws.Cells[1, 1, 2, properties.Count]; // Apply filter only to header rows for a template
        ws.Cells[1, 1, dataRowRange, properties.Count].AutoFitColumns();

        for (int col = 1; col <= properties.Count; col++)
        {
            ws.Column(col).Width = Math.Min(ws.Column(col).Width, 100);
        }

        ws.Row(1).Style.Font.Bold = true;
        ws.Row(2).Style.Font.Bold = true;
        ws.View.FreezePanes(3, 1);

        return package.GetAsByteArray();
    }
        
  
    
    

    public async Task<ImportResult<T>> ImportFromExcelAsync<T>(IBrowserFile file, string? sheetName = null) where T : class, new()
    {
        var items = new List<T>();
        var errors = new List<string>();

        if (file == null || file.Size == 0)
        {
            return new ImportResult<T>(items, 0, 0, new List<string> { "No file uploaded or file is empty." });
        }

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        try
        {
            await using var stream = file.OpenReadStream(file.Size);
            using var package = new ExcelPackage();
            await package.LoadAsync(stream);

            return await ProcessExcelPackageAsync<T>(package, sheetName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excel import failed");
            return new ImportResult<T>(items, 0, 1, new List<string> { $"Import failed: {ex.Message}" });
        }
    }

    public async Task<ImportResult<T>> ImportFromExcelAsync<T>(InputFileChangeEventArgs e, string? sheetName = null) where T : class, new()
    {
        if (e?.File == null)
        {
            return new ImportResult<T>(new List<T>(), 0, 0, new List<string> { "No file selected." });
        }

        return await ImportFromExcelAsync<T>(e.File, sheetName);
    }

    public byte[] ExportToExcel<T>(List<T>? items) where T : new()
    {
        items ??= new List<T> { new T() };

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add(typeof(T).Name);

        // Get exportable properties (exclude those marked with ExcludeFromExcelExport)
        var properties = typeof(T).GetProperties()
            .Where(p => !p.GetCustomAttributes(typeof(ExcludeFromExcelExportAttribute), false).Any())
            .OrderBy(p => p.GetCustomAttribute<DisplayAttribute>()?.GetOrder() ?? int.MaxValue)
            .ToList();

        // Write headers
        for (int col = 0; col < properties.Count; col++)
        {
            var displayName = properties[col].Name;
            ws.Cells[1, col + 1].Value = displayName;
        }
        for (int col = 0; col < properties.Count; col++)
        {
            var displayName = properties[col].GetCustomAttribute<DisplayAttribute>()?.Name ?? properties[col].Name;
            ws.Cells[2, col + 1].Value = displayName;
        }

        // Write data
        for (int row = 0; row < items.Count; row++)
        {
            for (int col = 0; col < properties.Count; col++)
            {
                ws.Cells[row + 3, col + 1].Value = properties[col].GetValue(items[row]);
            }
        }

        // Format
        var range = ws.Cells[1, 1, items.Count + 1, properties.Count];
        range.AutoFilter = true;
        range.AutoFitColumns();

        for (int col = 1; col <= properties.Count; col++)
        {
            ws.Column(col).Width = Math.Min(ws.Column(col).Width, 100);
        }

        ws.Row(1).Style.Font.Bold = true;
        ws.View.FreezePanes(2, 1);

        return package.GetAsByteArray();
    }

    #endregion

    #region Utility Methods

    public List<T> AssignSequence<T>(List<T> items) where T : class
    {
        if (items == null || items.Count == 0) return items ?? new List<T>();

        var seqProp = typeof(T).GetProperty("Seq");
        if (seqProp == null) return items;

        if (seqProp.PropertyType != typeof(int) && seqProp.PropertyType != typeof(int?))
            return items;

        var maxSeq = items
            .Select(i => seqProp.GetValue(i))
            .Where(v => v != null && Convert.ToInt32(v) != 0)
            .DefaultIfEmpty(0)
            .Max(v => Convert.ToInt32(v)) + 1;

        foreach (var item in items)
        {
            var currentSeq = Convert.ToInt32(seqProp.GetValue(item) ?? 0);
            if (currentSeq == 0)
            {
                seqProp.SetValue(item, maxSeq++);
            }
        }

        return items.OrderBy(i => Convert.ToInt32(seqProp.GetValue(i) ?? 0)).ToList();
    }

    #endregion

    #region Database Copy Operations

    public async Task CopyTablesAsync(string sourceConnectionString, string destConnectionString, IEnumerable<string> tableNames)
    {
        foreach (var tableName in tableNames)
        {
            try
            {
                await using var sourceConn = new SqlConnection(sourceConnectionString);
                await using var destConn = new SqlConnection(destConnectionString);

                await sourceConn.OpenAsync();
                await destConn.OpenAsync();

                // Get schema
                var schema = await GetTableSchemaAsync(sourceConn, tableName);

                // Drop and recreate destination table
                await DropTableIfExistsAsync(destConn, tableName);
                await CreateTableFromSchemaAsync(destConn, tableName, schema);

                // Copy data
                await CopyTableDataAsync(sourceConn, destConn, tableName);

                _logger.LogInformation("Copied table {Table} successfully", tableName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to copy table {Table}", tableName);
                // Continue with other tables
            }
        }
    }

    #endregion

    #region Private Helper Methods

    private static PropertyInfo? GetUidProperty<T>()
    {
        var type = typeof(T);
        return type.GetProperty(UID_FIELD, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
               ?? type.GetProperty("Uid", BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
               ?? type.GetProperty("Id", BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
    }

    private static Guid EnsureUid<T>(T item, PropertyInfo uidProp)
    {
        var uidValue = uidProp.GetValue(item);

        if (uidValue is Guid guid && guid != Guid.Empty)
            return guid;

        var newUid = Guid.NewGuid();
        uidProp.SetValue(item, newUid);
        return newUid;
    }

    private static void SetAuditFields<T>(T item, string updatedBy)
    {
        var type = typeof(T);

        var updatedOnProp = type.GetProperty(UPDATED_ON_FIELD, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        if (updatedOnProp?.PropertyType == typeof(DateTime))
        {
            updatedOnProp.SetValue(item, DateTime.Now);
        }

        var updatedByProp = type.GetProperty(UPDATED_BY_FIELD, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        if (updatedByProp?.PropertyType == typeof(string))
        {
            updatedByProp.SetValue(item, updatedBy);
        }
    }

    private static List<string> FindChangedFields<T>(T current, T original, List<string> dbColumns)
    {
        var changed = new List<string>();
        var type = typeof(T);

        foreach (var columnName in dbColumns)
        {
            var prop = type.GetProperty(columnName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (prop == null) continue;

            var newVal = prop.GetValue(current);
            var oldVal = prop.GetValue(original);

            if (!Equals(newVal, oldVal))
            {
                changed.Add(columnName);
            }
        }

        return changed;
    }

    private static DynamicParameters BuildParameters<T>(T item, List<string> fields, PropertyInfo uidProp)
    {
        var parameters = new DynamicParameters();
        var type = typeof(T);

        foreach (var field in fields)
        {
            var prop = type.GetProperty(field, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (prop != null)
            {
                parameters.Add($"@{field}", prop.GetValue(item));
            }
        }

        parameters.Add("@UID", uidProp.GetValue(item));
        return parameters;
    }

    private (string whereClause, Dictionary<string, object?> parameters) BuildWhereClause<T>(T item)
    {
        var type = typeof(T);
        var parameters = new Dictionary<string, object?>();

        // Try UID first
        var uidProp = GetUidProperty<T>();
        if (uidProp != null)
        {
            var uidValue = uidProp.GetValue(item);
            if (uidValue is Guid guid && guid != Guid.Empty)
            {
                parameters["@UID"] = guid;
                return ($"[{UID_FIELD}] = @UID", parameters);
            }
        }

        // Try [Key] attribute
        var keyProp = type.GetProperties()
            .FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null);
        if (keyProp != null)
        {
            var val = keyProp.GetValue(item);
            if (val != null)
            {
                parameters["@Key"] = val;
                return ($"[{keyProp.Name}] = @Key", parameters);
            }
        }

        // Fallback to common unique fields
        var conditions = new List<string>();
        foreach (var fieldName in UniqueFieldCandidates)
        {
            var prop = type.GetProperty(fieldName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (prop == null) continue;

            var val = prop.GetValue(item);
            if (val == null || string.IsNullOrWhiteSpace(val.ToString())) continue;

            var paramName = $"@{fieldName}";
            parameters[paramName] = val;
            conditions.Add($"[{fieldName}] = {paramName}");
        }

        return conditions.Count > 0
            ? (string.Join(" AND ", conditions), parameters)
            : (string.Empty, parameters);
    }

    private static (string propertyName, object? value) ExtractPropertyAndValue<T>(Expression<Func<T, bool>> predicate)
    {
        if (predicate.Body is not BinaryExpression { NodeType: ExpressionType.Equal } binExpr)
            throw new NotSupportedException("Only simple equality checks (p => p.Property == value) are supported.");

        MemberExpression? memberExpr = null;
        Expression? valueExpr = null;

        if (binExpr.Left is MemberExpression leftMember && leftMember.Expression is ParameterExpression)
        {
            memberExpr = leftMember;
            valueExpr = binExpr.Right;
        }
        else if (binExpr.Right is MemberExpression rightMember && rightMember.Expression is ParameterExpression)
        {
            memberExpr = rightMember;
            valueExpr = binExpr.Left;
        }

        if (memberExpr == null || valueExpr == null)
            throw new NotSupportedException("Expression must compare a property to a value.");

        var propertyName = memberExpr.Member.Name;

        object? value;
        try
        {
            var lambda = Expression.Lambda<Func<object>>(Expression.Convert(valueExpr, typeof(object)));
            value = lambda.Compile()();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to extract comparison value: {ex.Message}", ex);
        }

        return (propertyName, value);
    }

    private DataTable ToDataTable<T>(List<T> data)
    {
        var table = new DataTable(typeof(T).Name);
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Get column names synchronously (we're already in a sync context for bulk copy)
        List<string> dbColumns;
        lock (_cacheLock)
        {
            if (!_columnCache.TryGetValue(typeof(T).Name, out dbColumns!))
            {
                dbColumns = new List<string>();
            }
        }

        foreach (var prop in properties)
        {
            if (!dbColumns.Contains(prop.Name, StringComparer.OrdinalIgnoreCase)) continue;

            var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            table.Columns.Add(prop.Name, propType);
        }

        foreach (var item in data)
        {
            var row = table.NewRow();
            foreach (DataColumn col in table.Columns)
            {
                var prop = properties.First(p => p.Name == col.ColumnName);
                row[col.ColumnName] = prop.GetValue(item) ?? DBNull.Value;
            }
            table.Rows.Add(row);
        }

        return table;
    }

  private async Task<ImportResult<T>> ProcessExcelPackageAsync<T>(ExcelPackage package, string? sheetName) where T : class, new()
{
    var items = new List<T>();
    var errors = new List<string>();

    var ws = string.IsNullOrEmpty(sheetName)
        ? package.Workbook.Worksheets.FirstOrDefault()
        : package.Workbook.Worksheets[sheetName] ?? package.Workbook.Worksheets.FirstOrDefault();

    if (ws == null)
    {
        return new ImportResult<T>(items, 0, 0, new List<string> { "No worksheet found." });
    }

    var rowCount = ws.Dimension?.Rows ?? 0;
    var colCount = ws.Dimension?.Columns ?? 0;

    if (rowCount <= 1 || colCount == 0)
    {
        return new ImportResult<T>(items, 0, 0, new List<string> { "No data rows found." });
    }

    // Get current user context
    string currentUser = "system";
    string? currentProjectId = null;
    try
    {
        var (userName, projectId) = await _userContext.GetContextAsync();
        currentUser = userName ?? "system";
        currentProjectId = projectId;
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Failed to get user context for import. Using default.");
    }

    // Get all properties, excluding those marked with [ExcludeFromExcelImport] 
    // or in the default excluded list
    var properties = typeof(T).GetProperties()
        .Where(p => !p.GetCustomAttributes<ExcludeFromExcelImportAttribute>().Any())
        .Where(p => !DefaultImportExcludedFields.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
        .ToArray();

    // Map headers to properties
    var headerMap = new Dictionary<int, PropertyInfo>();

    for (int col = 1; col <= colCount; col++)
    {
        var header = ws.Cells[1, col].Value?.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(header)) continue;

        var prop = properties.FirstOrDefault(p =>
            (p.GetCustomAttribute<DisplayAttribute>()?.Name ?? p.Name)
                .Equals(header, StringComparison.OrdinalIgnoreCase)
            || p.Name.Equals(header, StringComparison.OrdinalIgnoreCase));

        if (prop != null) headerMap[col] = prop;
    }

    // Get system properties separately (we set these, not from Excel)
    var uidProp = typeof(T).GetProperty(UID_FIELD, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
    var updatedByProp = typeof(T).GetProperty(UPDATED_BY_FIELD, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
    var updatedOnProp = typeof(T).GetProperty(UPDATED_ON_FIELD, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
    var projectIdProp = typeof(T).GetProperty(PROJECT_ID_FIELD, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

    // Process rows from row 3 as row 1 has long field, row 2 has short field
    for (int row = 3; row <= rowCount; row++)
    {
        try
        {
            var instance = new T();

            foreach (var (col, prop) in headerMap)
            {
                var cellValue = ws.Cells[row, col].Value;
                if (cellValue == null) continue;

                try
                {
                    var convertedValue = ConvertCellValue(cellValue, prop.PropertyType);
                    prop.SetValue(instance, convertedValue);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Cell [{row},{col}] {prop.Name} : conversion to {prop.PropertyType.Name} failed: {ex.Message}");
                }
            }

            // Auto-generate UID (always generate new, ignore any from Excel)
            if (uidProp != null && uidProp.CanWrite)
            {
                uidProp.SetValue(instance, Guid.NewGuid());
            }

            // Set ProjectId from current context (if not already set from Excel)
            if (projectIdProp != null && projectIdProp.CanWrite && !string.IsNullOrEmpty(currentProjectId))
            {
                var existingProjectId = projectIdProp.GetValue(instance)?.ToString();
                if (string.IsNullOrEmpty(existingProjectId))
                {
                    projectIdProp.SetValue(instance, currentProjectId);
                }
            }

            // Set audit fields with current values
            if (updatedByProp != null && updatedByProp.CanWrite)
            {
                updatedByProp.SetValue(instance, currentUser);
            }

            if (updatedOnProp != null && updatedOnProp.CanWrite)
            {
                updatedOnProp.SetValue(instance, DateTime.Now);
            }

            items.Add(instance);
        }
        catch (Exception ex)
        {
            errors.Add($"Row {row}: {ex.Message}");
            _logger.LogWarning(ex, "Failed to import row {Row}", row);
        }
    }

    _logger.LogInformation(
        "Excel import completed for {Type} by {User}: {SuccessCount} succeeded, {ErrorCount} failed",
        typeof(T).Name, currentUser, items.Count, errors.Count);

    return new ImportResult<T>(items, items.Count, errors.Count, errors);
}
    private static object? ConvertCellValue(object cellValue, Type targetType)
    {
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlyingType == typeof(string))
            return cellValue.ToString();

        if (underlyingType == typeof(int))
            return Convert.ToInt32(cellValue);

        if (underlyingType == typeof(long))
            return Convert.ToInt64(cellValue);

        if (underlyingType == typeof(double))
            return Convert.ToDouble(cellValue);

        if (underlyingType == typeof(decimal))
            return Convert.ToDecimal(cellValue);

        if (underlyingType == typeof(float))
            return Convert.ToSingle(cellValue);

        if (underlyingType == typeof(bool))
            return Convert.ToBoolean(cellValue);

        if (underlyingType == typeof(DateTime))
        {
            if (DateTime.TryParseExact(cellValue.ToString(), DateTimeFormats,
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                return date;

            return Convert.ToDateTime(cellValue);
        }

        if (underlyingType == typeof(Guid))
            return Guid.Parse(cellValue.ToString()!);

        return Convert.ChangeType(cellValue, underlyingType);
    }

    private async Task<DataTable> GetTableSchemaAsync(SqlConnection connection, string tableName)
    {
        var sql = @"SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH, 
                           NUMERIC_PRECISION, NUMERIC_SCALE
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = @TableName
                    ORDER BY ORDINAL_POSITION";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@TableName", tableName);

        await using var reader = await cmd.ExecuteReaderAsync();
        var dt = new DataTable();
        dt.Load(reader);
        return dt;
    }

    private async Task DropTableIfExistsAsync(SqlConnection connection, string tableName)
    {
        var sql = $@"IF OBJECT_ID('{tableName}', 'U') IS NOT NULL DROP TABLE [{tableName}]";
        await using var cmd = new SqlCommand(sql, connection);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task CreateTableFromSchemaAsync(SqlConnection connection, string tableName, DataTable schema)
    {
        var columns = new List<string>();

        foreach (DataRow row in schema.Rows)
        {
            var colName = row["COLUMN_NAME"].ToString();
            var dataType = row["DATA_TYPE"].ToString();
            var isNullable = row["IS_NULLABLE"].ToString();
            var maxLength = row["CHARACTER_MAXIMUM_LENGTH"];

            var colDef = $"[{colName}] {dataType}";

            if (maxLength != DBNull.Value && int.TryParse(maxLength.ToString(), out var len))
            {
                colDef += len == -1 ? "(MAX)" : $"({len})";
            }

            if (isNullable == "NO") colDef += " NOT NULL";

            columns.Add(colDef);
        }

        var sql = $"CREATE TABLE [{tableName}] ({string.Join(", ", columns)})";
        await using var cmd = new SqlCommand(sql, connection);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task CopyTableDataAsync(SqlConnection source, SqlConnection dest, string tableName)
    {
        await using var selectCmd = new SqlCommand($"SELECT * FROM [{tableName}]", source);
        await using var reader = await selectCmd.ExecuteReaderAsync();

        using var bulkCopy = new SqlBulkCopy(dest)
        {
            DestinationTableName = $"[{tableName}]"
        };

        await bulkCopy.WriteToServerAsync(reader);
    }

    #endregion
    
    
    
}

#endregion

#region Attributes

/// <summary>
/// Mark properties to exclude from Excel export
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ExcludeFromExcelExportAttribute : Attribute { }

#endregion
