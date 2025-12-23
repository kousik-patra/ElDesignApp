using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using ElDesignApp.Services.Global;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Data.SqlClient;
using Dapper;
using ElDesignApp.Services.Infrastructure;
using System.Threading.Tasks;
using ElDesignApp.Models;
using ElDesignApp.Services.Cache;
using OfficeOpenXml;

namespace ElDesignApp.Services.DataBase;

public interface ITableService
{
    Task<List<string>> GetSqlFieldsNamesAsync(string tableName);

    Task<List<T>?> LoadDataAsync<T, U>(string sql, U parameters);
    Task<int> SaveDataAsync<T>(string sql, T parameters, int? timeout = null);
    Task DeleteDataAsync<T>(string sql);

    Task<int> InsertItemAsync<T>(T item) where T : class;

    Task<int> UpdateParameterAsync<T>(T item, params string[]? fields);
    Task<int> UpdateParameterAsync<T>(T item, Guid uid, List<string> fields);

    Task UpdateParameterItems<T>(T item, string uidstrings, List<string> fields);

    Task<(int AddedRows, int modifiedRows, int DeletedRows, ILogger logger)> UpdateAsync<T>(List<T> list, List<T> originalList, string user) where T : class;

    Task<int> DeleteItemAsync<T>(T item, Guid uid) where T : class;


    Task<bool> AnyAsync<T>(System.Linq.Expressions.Expression<Func<T, bool>> predicate) where T : class;
    List<T> AssignSequenceToList<T>(List<T> items);

    Task<List<T>?> GetListAsync<T>(T item, string projectId = "");

    // Excel
    Task<(List<T> Items, string Summary)> ImportFromExcel<T>(InputFileChangeEventArgs e, T item);
    byte[] GenerateExcelWorkbookByte<T>(List<T>? list);
    Task ExportExcelList<T>(List<T> items);

    // Bulk
    Task BulkCopyDataTableAsync<T>(List<T>? list, string project = "TestProject");

    // DB Copy / schema tools
    Task<List<string>> GetTablesAsync(string connectionString);
    Task CopyTablesAsync(string sourceConnection, string destConnection, List<string> tablesToCopy);
    Task<DataTable> GetTableSchemaAsync(SqlConnection connection, string tableName);
    Task CreateTableAsync(SqlConnection connection, string tableName, DataTable schemaTable);
    Task DropTableIfExistsAsync(SqlConnection connection, string tableName);
    Task CopyDataAsync(SqlConnection source, SqlConnection dest, string tableName);
}
    public class TableService : ITableService
{


    
    private readonly IConfiguration _configuration;
    private readonly IDbConnection _injectedConnection; // keep injected connection (do not overwrite)
    private readonly ICacheService _cache;
    private readonly IGlobalDataService _globalData;
    private readonly ILogger<TableService> _logger;
    private readonly IDbConnection _connection;
    private readonly IMiscService _misc;
    private readonly string _connectionString;
    private readonly string _dbProviderName; // "SqlServer" or "Postgres" etc.
    private string loginUser;
    private string? projectId;
    
    // Audit field names (customize if your entities use different names)
    private const string UID_FIELD = "UID";
    private const string PROJECT_ID_FIELD = "ProjectId";
    private const string UPDATED_BY_FIELD = "UpdatedBy";
    private const string UPDATED_ON_FIELD = "UpdatedOn";
    
    
    public TableService(
        IGlobalDataService globalData,
        IConfiguration configuration,
        ICacheService cache,
        IMiscService misc,
        ILogger<TableService> logger,
        IDbConnection connection)
    {
        
        _globalData = globalData ?? throw new ArgumentNullException(nameof(globalData));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _cache = cache;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _injectedConnection = connection ?? throw new ArgumentNullException(nameof(connection));

        _connectionString = _configuration.GetConnectionString("DefaultConnection")
                            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

        // Determine provider name heuristically from the provided connection
        _dbProviderName = _injectedConnection.GetType().Name.ToLowerInvariant().Contains("npgsql")
            ? "Postgres"
            : "SqlServer";
        _misc = misc;

    }

    private IDbConnection CreateNewConnection()
    {
        // Use the same type as the injected connection if possible (safer for DI usage)
        // If the injected connection is a SqlConnection, create a new SqlConnection using the configured connection string.
        var type = _injectedConnection.GetType();
        try
        {
            // Many ADO.NET connection types have a constructor accepting connectionString
            return (IDbConnection)Activator.CreateInstance(type, _connectionString)!;
        }
        catch
        {
            // Fallback: if we cannot create the same type dynamically, default to SqlConnection.
            return new SqlConnection(_connectionString);
        }
    }
    
    private async Task<T> WithConnection<T>(Func<IDbConnection, Task<T>> action, bool openIfClosed = true)
    {
        // Use a new connection per operation to avoid sharing state; keeps code safe if DI provided connection was already used elsewhere.
        using var conn = CreateNewConnection();
        try
        {
            if (openIfClosed && conn.State != ConnectionState.Open)
                await ((DbConnection)conn).OpenAsync();

            return await action(conn);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database operation failed.");
            throw; // Let caller handle friendly UI message
        }
        finally
        {
            try { if (conn.State != ConnectionState.Closed) conn.Close(); } catch { }
        }
    }

    private async Task<int> ExecuteAsync(IDbConnection conn, string sql, object? parameters = null, int? commandTimeout = null)
    {
        // Single place to wrap Dapper ExecuteAsync with logging
        try
        {
            return await conn.ExecuteAsync(new CommandDefinition(sql, parameters, commandTimeout: commandTimeout ?? 30));
        }
        catch (DbException dbex)
        {
            _logger.LogError(dbex, "Execute failed. SQL: {Sql}", sql);
            throw;
        }
    }

    private async Task<IEnumerable<T>> QueryAsync<T>(IDbConnection conn, string sql, object? parameters = null)
    {
        try
        {
            return await conn.QueryAsync<T>(sql, parameters);
        }
        catch (DbException dbex)
        {
            _logger.LogError(dbex, "Query failed. SQL: {Sql}", sql);
            throw;
        }
    }
    
    public async Task<List<string>> GetSqlFieldsNamesAsync(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName)) return new List<string>();

        // Use caching if available (simple in-memory via ICacheService)
        var cacheKey = $"cols::{tableName}::{_dbProviderName}";
        try
        {
            if (_cache != null)
            {
                var cached = await _cache.GetRecordAsync<List<string>>(cacheKey);
                if (cached != null && cached.Count > 0) return cached;
            }
        }
        catch { /* not critical */ }

        var sql = _dbProviderName switch
        {
            "Postgres" => @"
                    SELECT column_name
                    FROM information_schema.columns
                    WHERE table_name = @TableName
                      AND table_schema = 'public'
                    ORDER BY ordinal_position;",
            _ => @"
                    SELECT COLUMN_NAME
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = @TableName
                    ORDER BY ORDINAL_POSITION;"
        };

        var result = await WithConnection(async conn =>
        {
            var cols = await QueryAsync<string>(conn, sql, new { TableName = tableName });
            return cols.Select(s => s ?? string.Empty).Where(s => s.Length > 0).ToList();
        });

        try
        {
            if (_cache != null && result.Count > 0)
                await _cache.SetRecordAsync(cacheKey, result, TimeSpan.FromMinutes(30));
        }
        catch { /* not critical */ }

        return result;
    }
    
    
    
    public async Task<List<T>?> LoadDataAsync<T, TU>(string sql, TU parameters)
    {
        return await WithConnection(async conn =>
        {
            var rows = await QueryAsync<T>(conn, sql, parameters);
            return rows.ToList();
        });
    }

    public async Task<int> SaveDataAsync<T>(string sql, T parameters, int? commandTimeout = null)
    {
        return await WithConnection(async conn => await ExecuteAsync(conn, sql, parameters, commandTimeout));
    }


 
    public async Task DeleteDataAsync<T>(string sql)
    {
        await WithConnection(async conn => await ExecuteAsync(conn, sql));
    }

    
    // ---------- Insert / Update primitives ----------
    // Build a parameter object that contains only properties that map to DB columns
    private object BuildParameterObjectFromItem<T>(T item, IEnumerable<string> dbColumns)
    {
        var dyn = new DynamicParameters();
        var props = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);

        foreach (var prop in props)
        {
            if (!dbColumns.Contains(prop.Name)) continue;

            var val = prop.GetValue(item);
            dyn.Add("@" + prop.Name, val);
        }

        return dyn;
    }

    public async Task<int> InsertItemAsync<T>(T item) where T : class
    {
        if (item == null) throw new ArgumentNullException(nameof(item));

        var tableName = typeof(T).Name;
        var columns = await GetSqlFieldsNamesAsync(tableName);
        if (columns == null || columns.Count == 0)
            throw new InvalidOperationException($"No columns found for '{tableName}'");

        // Use only columns that are properties on T
        var props = typeof(T).GetProperties().Where(p => columns.Contains(p.Name)).ToList();
        if (!props.Any()) throw new InvalidOperationException($"No mappable properties found for '{tableName}'");

        var colList = props.Select(p => QuoteIdentifier(p.Name)).ToArray();
        var paramList = props.Select(p => "@" + p.Name).ToArray();

        var sql = $"INSERT INTO {QuoteIdentifier(tableName)} ({string.Join(", ", colList)}) VALUES ({string.Join(", ", paramList)});";

        var parameters = new DynamicParameters();
        foreach (var p in props) parameters.Add(p.Name, p.GetValue(item));

        return await WithConnection(async conn => await ExecuteAsync(conn, sql, parameters));
    }
    
// Update specific fields of an object identified by UID (preferred)
    public async Task<int> UpdateParameterAsync<T>(T item, params string[]? fields)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        if (fields == null || fields.Length == 0) return 0;

        var tableName = typeof(T).Name;
        var dbCols = await GetSqlFieldsNamesAsync(tableName);

        // Validate fields and ensure they exist in DB
        var allowed = fields.Where(f => dbCols.Contains(f)).Distinct().ToList();
        if (!allowed.Any()) return 0;

        // Identify UID (or fallback unique keys)
        var (whereClause, paramProvider) = BuildUniqueWhereClauseForItem(item);
        if (string.IsNullOrEmpty(whereClause)) throw new InvalidOperationException("Cannot determine WHERE clause for UpdateParameterAsync. Provide UID or unique field.");

        var setClauses = allowed.Select(f => $"{QuoteIdentifier(f)} = @{f}");
        var sql = $"UPDATE {QuoteIdentifier(tableName)} SET {string.Join(", ", setClauses)} WHERE {whereClause};";

        var parameters = new DynamicParameters();
        // add fields from item
        foreach (var f in allowed)
        {
            var prop = typeof(T).GetProperty(f);
            parameters.Add("@" + f, prop?.GetValue(item));
        }

        // add where clause params (UID or unique fields)
        foreach (var kv in paramProvider())
        {
            parameters.Add(kv.Key, kv.Value);
        }

        return await WithConnection(async conn => await ExecuteAsync(conn, sql, parameters));
    }

    public Task<int> UpdateParameterAsync<T>(T item, Guid uid, List<string> fields)
    {
        throw new NotImplementedException();
    }

    public Task UpdateParameterItems<T>(T item, string uidstrings, List<string> fields)
    {
        throw new NotImplementedException();
    }


    // Update list items (single function to manage add/update/delete). Uses UID as primary identifier.
 
    public async Task<(int AddedRows, int modifiedRows, int DeletedRows, ILogger logger)> UpdateAsync<T>(List<T> list, List<T> originalList, string user) where T : class
{
    list ??= new List<T>();
    originalList ??= new List<T>();


    var (_, userName) = await _misc.GetCurrentUserInfoAsync();
    loginUser = userName ?? "Guest";

    projectId = _globalData.SelectedProject?.Tag?.Trim() ?? "New Project";
    
    int deletedRows = 0;
    int addedRows = 0;
    int modifiedRows = 0;

    var type = typeof(T);
    var tableName = type.Name;

    // Find UID property (case-insensitive)
    var uidProp = type.GetProperty(UID_FIELD, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
                  ?? type.GetProperty("Uid", BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
                  ?? type.GetProperty("Id", BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

    if (uidProp == null)
        throw new InvalidOperationException($"Type {type.Name} must have a UID, Uid, or Id property.");

    if (uidProp.PropertyType != typeof(Guid) && uidProp.PropertyType != typeof(Guid?))
        throw new InvalidOperationException($"UID property must be of type Guid or Guid?.");

    var dbColumns = await GetSqlFieldsNamesAsync(tableName);

    // Build lookup dictionary from original list
    var originalByUid = originalList
        .Where(item => uidProp.GetValue(item) is Guid uid && uid != Guid.Empty)
        .ToDictionary(
            item => (Guid)uidProp.GetValue(item)!,
            item => item
        );

    await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
    await connection.OpenAsync();
    await using var transaction = connection.BeginTransaction();

    try
    {
        var processedUids = new HashSet<Guid>();

        foreach (var item in list)
        {
            var uidObj = uidProp.GetValue(item);
            Guid uid;

            if (uidObj is Guid g && g != Guid.Empty)
            {
                uid = g;
            }
            else
            {
                uid = Guid.NewGuid();
                uidProp.SetValue(item, uid);
            }

            if (originalByUid.TryGetValue(uid, out var originalItem))
            {
                // UPDATE
                var changedFields = new List<string>();

                foreach (var col in dbColumns)
                {
                    var prop = type.GetProperty(col);
                    if (prop == null) continue;

                    var newVal = prop.GetValue(item);
                    var oldVal = prop.GetValue(originalItem);

                    if (!Equals(newVal, oldVal))
                        changedFields.Add(col);
                }

                if (changedFields.Count > 0)
                {
                    // Auto-set audit fields if they exist
                    SetAuditFields(item);

                    var setClauses = changedFields.Select(c => $"[{c}] = @{c}");
                    var sql = $"UPDATE dbo.[{tableName}] SET {string.Join(", ", setClauses)} WHERE [UID] = @UID";

                    var parameters = new DynamicParameters();
                    foreach (var field in changedFields)
                    {
                        var prop = type.GetProperty(field)!;
                        parameters.Add($"@{field}", prop.GetValue(item));
                    }
                    parameters.Add("@UID", uid);

                    modifiedRows = await connection.ExecuteAsync(sql, parameters, transaction);
                }

                processedUids.Add(uid);
            }
            else
            {
                // INSERT
                SetAuditFields(item);

                var insertableProps = type.GetProperties()
                    .Where(p => dbColumns.Contains(p.Name))
                    .ToList();

                var columns = insertableProps.Select(p => $"[{p.Name}]");
                var values = insertableProps.Select(p => $"@{p.Name}");

                var sql = $"INSERT INTO dbo.[{tableName}] ({string.Join(", ", columns)}) VALUES ({string.Join(", ", values)})";

                var parameters = new DynamicParameters();
                foreach (var prop in insertableProps)
                {
                    parameters.Add($"@{prop.Name}", prop.GetValue(item));
                }

                addedRows = await connection.ExecuteAsync(sql, parameters, transaction);
            }
        }

        // DELETE removed items
        var currentUids = list
            .Select(item => uidProp.GetValue(item))
            .OfType<Guid>()
            .Where(g => g != Guid.Empty)
            .ToHashSet();

        var toDelete = originalByUid.Keys.Except(currentUids).ToList();

        foreach (var uid in toDelete)
        {
            var sql = $"DELETE FROM dbo.[{tableName}] WHERE [UID] = @UID";
            deletedRows += await connection.ExecuteAsync(sql, new { UID = uid }, transaction);
        }

        transaction.Commit();
    }
    catch (Exception ex)
    {
        transaction.Rollback();
        _logger.LogError(ex, "UpdateAsync failed for {Type}: {Message}", type.Name, ex.Message);
        throw new ApplicationException($"Failed to update {type.Name}: {ex.Message}", ex);
    }
    return (addedRows, modifiedRows , deletedRows, _logger);
}


// Helper to auto-set UpdatedOn/UpdatedBy/ProjectId
        private void SetAuditFields<T>(T item) where T : class
        {
            
            var type = typeof(T);
            
            var projectIdProp = type.GetProperty(PROJECT_ID_FIELD, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (projectIdProp != null && projectIdProp.PropertyType == typeof(string))
            {
                projectIdProp.SetValue(item, projectId);
            }
            

            var updatedOnProp = type.GetProperty(UPDATED_ON_FIELD, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (updatedOnProp != null && updatedOnProp.PropertyType == typeof(DateTime))
            {
                updatedOnProp.SetValue(item, DateTime.Now);
            }

            var updatedByProp = type.GetProperty(UPDATED_BY_FIELD, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (updatedByProp != null && updatedByProp.PropertyType == typeof(string))
            {
                updatedByProp.SetValue(item, loginUser);
            }
        }

        public async Task<int> DeleteItemAsync<T>(T item, Guid uid) where T : class
        {
            var table = typeof(T).Name;
            var sql = $"DELETE FROM {QuoteIdentifier(table)} WHERE {QuoteIdentifier(UID_FIELD)} = @UID;";
            return await WithConnection(async conn => await ExecuteAsync(conn, sql, new { UID = uid }));
        }


        public async Task<bool> AnyAsync<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            // Extract property name and value from simple equality: p => p.Tag == "ABC"
            if (predicate.Body is not BinaryExpression { NodeType: ExpressionType.Equal } binExpr ||
                binExpr.Left is not MemberExpression memberExpr ||
                memberExpr.Expression is not ParameterExpression)
            {
                throw new NotSupportedException("Only simple equality checks like p => p.Tag == value are supported.");
            }

            var propertyName = memberExpr.Member.Name;
            var value = Expression.Lambda(predicate).Compile().DynamicInvoke();

            var tableName = typeof(T).Name;
            var sql = $"SELECT TOP 1 1 FROM dbo.[{tableName}] WHERE [{propertyName}] = @Value";

            var result = await _connection.QueryFirstOrDefaultAsync<int?>(sql, new { Value = value });
            return result == 1;
        }

    
    public List<T> AssignSequenceToList<T>(List<T> items)
    {
        if (items == null || items.Count == 0) return items ?? new List<T>();
        var seqProp = typeof(T).GetProperty("Seq");
        if (seqProp == null) return items;
        if (seqProp.PropertyType != typeof(int) && seqProp.PropertyType != typeof(int?))
            throw new InvalidOperationException("Seq property must be of type int or int?.");

        var existingMax = items.Select(i => seqProp.GetValue(i))
            .Where(v => v != null && Convert.ToInt32(v) != 0)
            .DefaultIfEmpty(0)
            .Max(v => Convert.ToInt32(v)) + 1;

        foreach (var item in items)
        {
            var cur = Convert.ToInt32(seqProp.GetValue(item) ?? 0);
            if (cur == 0) seqProp.SetValue(item, existingMax++);
        }

        return items.OrderBy(i => Convert.ToInt32(seqProp.GetValue(i))).ToList();
    }


    public async Task<List<T>?> GetListAsync<T>(T item, string selectedProject = "")
    {
        var tableName = typeof(T).Name;
        var sql = $"select * from dbo.{tableName}";
        // Tables that don't need project filtering
        var excludedTables = new[] 
        { 
            "RoleMapping",      
            "DBMaster",         
            "Project",
            "ProjectUserAssignment",
            "ProjectUserRole"
            
        };
        
        var containsData = tableName.Contains("Data");
        var isExcluded = excludedTables.Contains(tableName);
    
        // Project filter not applicable for catalogue data, Project tables, or excluded tables
        if (!containsData && !isExcluded) 
        {
            sql += " WHERE ProjectId = @ProjectId";
        }
    
        if (tableName == "DBMaster") 
        {
            sql += " OR ProjectId = ''";
        }
    
        if (tableName == "LoadMaster")
        {
            sql = "select * from dbo.MasterLoadList WHERE ProjectId = @ProjectId";
        }
        
        return await LoadDataAsync<T, dynamic>(sql, new { ProjectId = selectedProject}) ?? [];
    }
    
    

    public async Task<(List<T> Items, string Summary)> ImportFromExcel<T>(InputFileChangeEventArgs e, T item)
        {
            var importedItems = new List<T>();
            var summary = string.Empty;

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            if (e?.File == null || e.File.Size == 0)
            {
                summary = "No file uploaded or file is empty.";
                return (importedItems, summary);
            }

            try
            {
                using var stream = e.File.OpenReadStream(e.File.Size);
                using var package = new ExcelPackage();
                await package.LoadAsync(stream);
                var ws = package.Workbook.Worksheets.FirstOrDefault();
                if (ws == null) return (importedItems, "No worksheet found.");

                var rowCount = ws.Dimension?.Rows ?? 0;
                var colCount = ws.Dimension?.Columns ?? 0;
                if (rowCount <= 1 || colCount == 0) return (importedItems, "No data rows found in worksheet.");

                // Map headers to properties (DisplayAttribute preferred)
                var props = typeof(T).GetProperties();
                var headerMap = new Dictionary<int, PropertyInfo>();
                for (int c = 1; c <= colCount; c++)
                {
                    var header = ws.Cells[1, c].Value?.ToString()?.Trim();
                    if (string.IsNullOrWhiteSpace(header)) continue;

                    var prop = props.FirstOrDefault(p =>
                        (p.GetCustomAttribute<System.ComponentModel.DataAnnotations.DisplayAttribute>()?.Name ?? p.Name).Equals(header, StringComparison.OrdinalIgnoreCase)
                        || p.Name.Equals(header, StringComparison.OrdinalIgnoreCase));

                    if (prop != null) headerMap[c] = prop;
                }

                var uidProp = props.FirstOrDefault(p => p.Name == UID_FIELD);

                var failedRows = new List<string>();
                for (int r = 2; r <= rowCount; r++)
                {
                    try
                    {
                        var instance = Activator.CreateInstance<T>()!;
                        foreach (var kv in headerMap)
                        {
                            var cell = ws.Cells[r, kv.Key].Value;
                            if (cell == null) continue;

                            var tprop = kv.Value;
                            var targetType = Nullable.GetUnderlyingType(tprop.PropertyType) ?? tprop.PropertyType;
                            try
                            {
                                object? val = null;
                                if (targetType == typeof(string)) val = cell.ToString();
                                else if (targetType == typeof(int)) val = Convert.ToInt32(cell);
                                else if (targetType == typeof(double)) val = Convert.ToDouble(cell);
                                else if (targetType == typeof(decimal)) val = Convert.ToDecimal(cell);
                                else if (targetType == typeof(float)) val = Convert.ToSingle(cell);
                                else if (targetType == typeof(bool)) val = Convert.ToBoolean(cell);
                                else if (targetType == typeof(DateTime)) val = Convert.ToDateTime(cell);
                                else if (targetType == typeof(Guid)) val = Guid.Parse(cell.ToString()!);
                                else val = Convert.ChangeType(cell, targetType);

                                tprop.SetValue(instance, val);
                            }
                            catch
                            {
                                // ignore conversion error for this cell; mark row as failed
                                throw new InvalidOperationException($"Failed to convert cell [{r},{kv.Key}] to {tprop.PropertyType.Name}");
                            }
                        }

                        // Ensure UID exists or generate one
                        if (uidProp != null)
                        {
                            var uidVal = uidProp.GetValue(instance);
                            if (uidVal == null || uidVal.Equals(Guid.Empty))
                                uidProp.SetValue(instance, Guid.NewGuid());
                        }

                        importedItems.Add(instance);
                    }
                    catch (Exception rowEx)
                    {
                        _logger.LogWarning(rowEx, "Failed to import row {Row}", r);
                        failedRows.Add($"Row {r}: {rowEx.Message}");
                    }
                }

                summary = $"Imported: {importedItems.Count}, Failed: {failedRows.Count}";
                if (failedRows.Count > 0) summary += ". Failures: " + string.Join("; ", failedRows.Take(10));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ImportFromExcel failed");
                summary = "Import failed: " + ex.Message;
            }

            return (importedItems, summary);
        }


        public byte[] GenerateExcelWorkbookByte<T>(List<T>? list)
        {
            if (list == null || list.Count == 0)
                list = new List<T>() { Activator.CreateInstance<T>()! };

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add(typeof(T).Name);

            var props = typeof(T).GetProperties()
                .Where(p => !p.GetCustomAttributes(typeof(ExcludeFromExcelExportAttribute), false).Any())
                .OrderBy(p => p.GetCustomAttribute<DisplayAttribute>()?.Order ?? int.MaxValue)
                .ToList();

            for (int i = 0; i < props.Count; i++)
            {
                var displayName = props[i].GetCustomAttribute<DisplayAttribute>()?.Name ?? props[i].Name;
                ws.Cells[1, i + 1].Value = displayName;
            }

            for (int r = 0; r < list.Count; r++)
            {
                for (int c = 0; c < props.Count; c++)
                {
                    ws.Cells[r + 2, c + 1].Value = props[c].GetValue(list[r]);
                }
            }

            var range = ws.Cells[1, 1, list.Count + 1, props.Count];
            range.AutoFilter = true;
            range.AutoFitColumns();
            ws.Row(1).Style.Font.Bold = true;
            ws.View.FreezePanes(2, 1);

            return package.GetAsByteArray();
        }

        public async Task ExportExcelList<T>(List<T> items)
        {
            try
            {
                var file = new System.IO.FileInfo(@"E:\Dev\Data\Output\" + typeof(T).Name + "s.xlsx");
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                if (file.Exists) file.Delete();
                using var package = new ExcelPackage(file);
                var ws = package.Workbook.Worksheets.Add("List");
                var range = ws.Cells["A2"].LoadFromCollection(items, true);
                range.AutoFitColumns();
                ws.Row(1).Style.Font.Bold = true;
                await package.SaveAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExportExcelList failed");
                throw;
            }
        }

        public Task BulkCopyDataTableAsync<T>(List<T>? list, string project = "TestProject")
        {
            throw new NotImplementedException();
        }

        // ---------- Table/schema copy helpers ----------
        public async Task<List<string>> GetTablesAsync(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException(nameof(connectionString));
            return await WithConnection(async conn =>
            {
                var sql = _dbProviderName == "Postgres"
                    ? "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE';"
                    : "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE';";
                var tables = await QueryAsync<string>(conn, sql, null);
                return tables.ToList();
            });
        }

        public async Task CopyTablesAsync(string sourceConnectionString, string destinationConnectionString, List<string> tablesToCopy)
        {
            // High-level orchestration: iterate tables and copy schema + data
            foreach (var table in tablesToCopy)
            {
                try
                {
                    // Create connections for source and destination
                    await using var srcConn = new SqlConnection(sourceConnectionString);
                    await using var dstConn = new SqlConnection(destinationConnectionString);
                    if (srcConn.State != ConnectionState.Open) await srcConn.OpenAsync();
                    if (dstConn.State != ConnectionState.Open) await dstConn.OpenAsync();

                    var schema = await GetTableSchemaAsync((SqlConnection)srcConn, table); // NOTE: this method below expects SqlConnection; for Postgres you'd adapt
                    await DropTableIfExistsAsync((SqlConnection)dstConn, table);
                    await CreateTableAsync((SqlConnection)dstConn, table, schema);
                    await CopyDataAsync((SqlConnection)srcConn, (SqlConnection)dstConn, table);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to copy table {Table}", table);
                    // swallow to continue others; optionally rethrow if you want to halt on first error.
                }
            }
        }

        // The following three methods use SqlConnection types since schema copy was originally SQL Server specific.
        // If you need Postgres support here, we will adapt to Npgsql equivalents.
        public async Task<DataTable> GetTableSchemaAsync(SqlConnection connection, string tableName)
        {
            var schemaQuery = $@"
                SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = @tableName
                ORDER BY ORDINAL_POSITION";

            using var cmd = new SqlCommand(schemaQuery, connection);
            cmd.Parameters.AddWithValue("@tableName", tableName);
            using var reader = await cmd.ExecuteReaderAsync();
            var dt = new DataTable();
            dt.Load(reader);
            return dt;
        }

        public async Task CreateTableAsync(SqlConnection connection, string tableName, DataTable schemaTable)
        {
            var columns = new List<string>();
            foreach (DataRow row in schemaTable.Rows)
            {
                var columnName = row["COLUMN_NAME"].ToString();
                var dataType = row["DATA_TYPE"].ToString();
                var isNullable = row["IS_NULLABLE"].ToString();
                var maxLengthObj = row["CHARACTER_MAXIMUM_LENGTH"];
                string colDef = $"{QuoteIdentifier(columnName)} {dataType}";

                if (maxLengthObj != DBNull.Value && int.TryParse(maxLengthObj.ToString(), out var maxLen))
                {
                    if (maxLen == -1) colDef += "(MAX)";
                    else if (dataType.Equals("varchar", StringComparison.OrdinalIgnoreCase) || dataType.Equals("nvarchar", StringComparison.OrdinalIgnoreCase))
                        colDef += $"({maxLen})";
                }

                if (isNullable == "NO") colDef += " NOT NULL";
                columns.Add(colDef);
            }

            var createSql = $"CREATE TABLE {QuoteIdentifier(tableName)} ({string.Join(", ", columns)});";
            using var cmd = new SqlCommand(createSql, connection);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DropTableIfExistsAsync(SqlConnection connection, string tableName)
        {
            var sql = $@"
                IF OBJECT_ID('{tableName}', 'U') IS NOT NULL
                BEGIN
                    DROP TABLE {QuoteIdentifier(tableName)};
                END";
            using var cmd = new SqlCommand(sql, connection);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task CopyDataAsync(SqlConnection sourceConnection, SqlConnection destinationConnection, string tableName)
        {
            var select = new SqlCommand($"SELECT * FROM {QuoteIdentifier(tableName)}", sourceConnection);
            using var reader = await select.ExecuteReaderAsync();
            using var bulk = new SqlBulkCopy(destinationConnection)
            {
                DestinationTableName = tableName
            };
            await bulk.WriteToServerAsync(reader);
        }
    
    
private (string whereClause, Func<Dictionary<string, object?>> paramProvider) BuildUniqueWhereClauseForItem<T>(T item)
        {
            // Try UID property first
            var type = typeof(T);
            var uidProp = type.GetProperty(UID_FIELD) ?? type.GetProperty("Uid") ?? type.GetProperty("Id");
            if (uidProp != null && (uidProp.PropertyType == typeof(Guid) || uidProp.PropertyType == typeof(Guid?)))
            {
                var uidVal = uidProp.GetValue(item);
                if (uidVal is Guid g && g != Guid.Empty)
                {
                    return ($"{QuoteIdentifier(UID_FIELD)} = @UID", () => new Dictionary<string, object?> { { "@UID", g } });
                }
            }

            // Next try [Key] attribute
            var keyProp = type.GetProperties().FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null);
            if (keyProp != null)
            {
                var val = keyProp.GetValue(item);
                if (val != null) return ($"{QuoteIdentifier(keyProp.Name)} = @Key", () => new Dictionary<string, object?> { { "@Key", val } });
            }

            // Fallback to common unique fields
            var candidates = new[] { "Tag", "Code", "Name", "Email", "UserName", "ProjectCode", "ProjectTag", "LoginId" };
            var pairs = new List<string>();
            var paramDict = new Dictionary<string, object?>();
            foreach (var cand in candidates)
            {
                var p = type.GetProperty(cand);
                if (p == null) continue;
                var v = p.GetValue(item);
                if (v == null) continue;
                pairs.Add($"{QuoteIdentifier(cand)} = @{cand}");
                paramDict.Add("@" + cand, v);
            }

            if (pairs.Any())
            {
                var wc = string.Join(" AND ", pairs);
                return (wc, () => paramDict);
            }

            // Nothing found
            return (string.Empty, () => new Dictionary<string, object?>());
        }

    
    private string QuoteIdentifier(string identifier)
    {
        // Simple implementation for SQL Server
        return $"[{identifier}]";
    }
}