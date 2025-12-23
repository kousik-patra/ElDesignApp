using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

using ElDesignApp.Models;
using ElDesignApp.Services.Cache;
using ElDesignApp.Services.Global;
using Dapper;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearRegression;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using OfficeOpenXml;

namespace ElDesignApp.Services;




public interface IMyTableService
{
        Task<List<T>?> GetList<T>(T item, string selectedProject= "");

        Task<List<T>?> LoadData<T, U>(string sql, U parameters);
        Task<int> SaveDataAsync<T>(string sql, T parameters, int? commandTimeout = null);
        Task BulkCopyDataTableAsync<T>(List<T>? list, string selectedProject = "TestProject");
        Task<int> InsertItemAsync<T>(T item) where T : class;
        Task<bool> AnyAsync<T>(Expression<Func<T, bool>> predicate) where T : class;
        Task UpdateAsync<T>(List<T> list, List<T> originalList, string updatedBy = "KP") where T : class;
        Task SaveData<T>(string sql, T parameters);
        Task DeleteData<T>(string sql);
        Task Update<T>(List<T> list, List<T> originalList, String updatedBy = "KP");
        Task UpdateItem<T>(T item, Guid uid);
        Task<int> UpdateParameterAsync<T>(T item, params string[]? fields);
        Task UpdateParameter<T>(T item, Guid uid, List<string> fields);
        Task UpdateParameterItems<T>(T item, string uidstrings, List<string> fields);
        List<T> AssignSequenceToList<T>(List<T> items);
        Task<(List<T> Items, string Summary)> ImportFromExcel<T>(InputFileChangeEventArgs e, T item, string sheetName = null);
        Task<(List<T> Items, string Summary)> ImportFromExcel<T>(IBrowserFile? file, T item, string? sheetName = null);
        Task InsertItem<T>(T item);
        Task DeleteItem<T>(T item, Guid uid);
        byte[] GenerateExcelWorkbookByte<T>(List<T>? list);
        Task ExportExcelList<T>(List<T> items);
        Task DeleteItem1(string dboName, string field, string fieldValue);
        List<string> GetSqlFieldsNames(string tableName);
        Task<List<string>> GetTablesAsync(string connectionString);
        Task CopyTablesAsync(string sourceConnectionString, string destinationConnectionString,
            List<string> tablesToCopy);
        Task<DataTable> GetTableSchemaAsync(SqlConnection connection, string tableName);
        Task CreateTableAsync(SqlConnection connection, string tableName, DataTable schemaTable);
        Task DropTableIfExistsAsync(SqlConnection connection, string tableName);
        Task CopyDataAsync(SqlConnection sourceConnection, SqlConnection destinationConnection,
            string tableName);

}

public class SqlConnectionConfiguration
{
    public SqlConnectionConfiguration(string connectionString)
    {
        ConnectionString = connectionString;
    }

    public string ConnectionString { get; }
}


public class MyTableService : IMyTableService
    {
        
        private readonly IConfiguration _configuration;
        private readonly IDbConnection _connection;
        private readonly ICacheService _cache;
        private readonly IGlobalDataService _globalData;
        private readonly ILogger<DataRetrievalService> _logger;
        
        
        // Constructor for dependency injection
        public MyTableService(
            IGlobalDataService globalData,
            IConfiguration configuration,
            ICacheService cache,
            ILogger<DataRetrievalService> logger,
            IDbConnection  connection
            )
        {
            _cache = cache;
            _globalData = globalData;
            _configuration = configuration;
            GetConnectionString();
            _logger = logger;

            var connectionString = configuration.GetConnectionString("DefaultConnection")
                                   ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

            _connection = new SqlConnection(connectionString);
            
        }
        
        // Reusable Execute with return value
        private async Task<int> ExecuteAsync<T>(string sql, T parameters, int? commandTimeout = null)
        {
            try
            {
                return await _connection.ExecuteAsync(sql, parameters, commandTimeout: commandTimeout ?? 30);
            }
            catch (SqlException ex) when (ex.Number is 2627 or 2601)
            {
                _logger.LogWarning(ex, "Duplicate key: {Sql}", sql);
                throw new InvalidOperationException("A record with this key already exists.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SQL Error: {Sql}", sql);
                throw;
            }
        }

        private async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? parameters = null)
        {
            return await _connection.QueryFirstOrDefaultAsync<T>(sql, parameters);
        }
        
 /// <summary>
/// Simpler version that takes property name and value directly
/// </summary>
public async Task<bool> AnyAsync<T>(string propertyName, object value) where T : class
{
    var tableName = typeof(T).Name;
    var sql = $"SELECT TOP 1 1 FROM dbo.[{tableName}] WHERE [{propertyName}] = @Value";

    try
    {
        var result = await _connection.QueryFirstOrDefaultAsync<int?>(sql, new { Value = value });
        return result == 1;
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"AnyAsync SQL Error: {ex.Message}");
        throw new InvalidOperationException($"Database query failed for {tableName}.{propertyName}: {ex.Message}", ex);
    }
}

/// <summary>
/// Expression-based version for backward compatibility
/// </summary>
public async Task<bool> AnyAsync<T>(Expression<Func<T, bool>> predicate) where T : class
{
    // Extract property name and value from the expression
    var (propertyName, value) = ExtractPropertyAndValue(predicate);
    
    // Use the simpler overload
    return await AnyAsync<T>(propertyName, value);
}

private (string PropertyName, object? Value) ExtractPropertyAndValue<T>(Expression<Func<T, bool>> predicate)
{
    if (predicate.Body is not BinaryExpression { NodeType: ExpressionType.Equal } binExpr)
    {
        throw new NotSupportedException("Only simple equality checks like p => p.Tag == value are supported.");
    }

    // Determine which side is the property and which is the value
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
    else
    {
        throw new NotSupportedException("Expression must compare a property to a value.");
    }

    var propertyName = memberExpr.Member.Name;

    // Extract the value
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
 
        public async Task<int> InsertItemAsync<T>(T item) where T : class
        {
            var type = typeof(T);
            var tableName = type.Name;

            // CRITICAL CHANGE: Use the DB-driven function to get actual column names
            List<string> dbColumnNames = await GetSqlFieldsNamesAsync(tableName);

            // 1. Filter C# properties to include only those that match a database column name
            var propertiesToInsert = type.GetProperties()
                .Where(p => dbColumnNames.Contains(p.Name))
                .ToList();

            if (!propertiesToInsert.Any())
                throw new InvalidOperationException($"No matching database columns found for properties in type {tableName}");

            // 2. Build the SQL statement using the database column names and corresponding parameter names
            var columns = propertiesToInsert.Select(p => $"[{p.Name}]");
            var parameters = propertiesToInsert.Select(p => $"@{p.Name}");

            var sql = $"INSERT INTO dbo.[{tableName}] ({string.Join(", ", columns)}) " +
                      $"VALUES ({string.Join(", ", parameters)})";

            // 3. Pass the full 'item' to ExecuteAsync. Dapper will automatically find 
            //    matching parameters (@ColumnName) from the 'item' object. 
            //    It will ignore extra properties on 'item' that were not included in the SQL.
            return await ExecuteAsync(sql, item);
        }
       
        
        private string GetConnectionString()
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("No valid database connection string found. Ensure 'DefaultConnection' is configured in User Secrets, appsettings, or Azure.");
            }

            return connectionString;
        }
        
        
        /// <summary>
        /// Generates and executes a SQL UPDATE statement for specific changed fields on a single item.
        /// </summary>
        /// <typeparam name="T">The type of the item being updated.</typeparam>
        /// <param name="item">The object containing the new values.</param>
        /// <param name="uid">The unique identifier used in the WHERE clause.</param>
        /// <param name="changedFields">A list of property names that have changed.</param>
        private async Task<int> UpdateParameterAsync<T>(T item, Guid uid, List<string> changedFields) where T : class
        {
            // Define the expected field names as constants/strings
            const string updatedOnFieldName = "UpdatedOn";
            const string updatedByFieldName = "UpdatedBy";
    
            var type = typeof(T);
            var tableName = type.Name;
    
            // Get the updatedBy value from the item
            // Note: We assume the UpdateAsync method has already set the UpdatedBy property on 'item' 
            // before calling this helper, or we use the value provided in the UpdateAsync signature.
            var updatedByValue = type.GetProperty(updatedByFieldName)?.GetValue(item) as string;

            // 1. Identify all fields to set in the UPDATE statement
            var updateClauses = changedFields
                .Select(field => $"[{field}] = @{field}");

            // 2. Add UpdatedOn and UpdatedBy fields using their string names
            var updatedOnClause = $"[{updatedOnFieldName}] = @{updatedOnFieldName}";
            var updatedByClause = $"[{updatedByFieldName}] = @{updatedByFieldName}";
    
            var setClauses = new List<string> { updatedOnClause, updatedByClause };
            setClauses.AddRange(updateClauses);

            // 3. Construct the full SQL UPDATE statement
            var sql = $"UPDATE dbo.[{tableName}] SET {string.Join(", ", setClauses)} WHERE [UID] = @UID";

            // 4. Create a dynamic parameter object
            // Dapper will automatically map properties from 'item' if their names match the @parameters.
            // We use DynamicParameters here to add the non-mapped audit fields and the UID.
            var parameters = new DynamicParameters(item);
    
            // Explicitly add the necessary audit fields and the UID for the WHERE clause.
            parameters.Add("@UID", uid);
            parameters.Add($"@{updatedOnFieldName}", DateTime.Now);
            parameters.Add($"@{updatedByFieldName}", updatedByValue); 

            return await ExecuteAsync(sql, parameters);
        }
        
        public async Task UpdateAsync<T>(List<T> list, List<T> originalList, string updatedBy = "KP") where T : class
{
    // Validate properties
    var uidProperty = typeof(T).GetProperty("UID");
    var updatedByProperty = typeof(T).GetProperty("UpdatedBy");
    var updatedOnProperty = typeof(T).GetProperty("UpdatedOn");
    var save2DbProperty = typeof(T).GetProperty("Save2DB");

    if (uidProperty == null || updatedByProperty == null || updatedOnProperty == null)
    {
        throw new ArgumentException("Type T must have UID, UpdatedBy, and UpdatedOn properties.");
    }

    // Assuming you have a function GetSqlFieldsNames that returns property names that map to DB fields
    var fields = GetSqlFieldsNames(typeof(T).Name); 
    
    var add = new List<T>();
    var delete = new List<T>();

    // Log start
    Debug.WriteLine($"{DateTime.Now:HH:mm:ss.ffffff} - Update: Starting comparison for {list.Count} items.");

    // Handle empty originalList case
    if (!originalList.Any())
    {
        add.AddRange(list); // All items are new
    }
    else
    {
        // Identify items to delete (in originalList but not in list)
        delete.AddRange(originalList.Where(orig => 
            !list.Any(item => uidProperty.GetValue(item)?.ToString() == uidProperty.GetValue(orig)?.ToString())));

        // Compare and identify items to add or update
        var tasksCompare = list.Select(async item =>
        {
            var uid = uidProperty.GetValue(item)?.ToString();
            var originalItem = originalList.FirstOrDefault(p => uidProperty.GetValue(p)?.ToString() == uid);

            if (originalItem == null)
            {
                // New item
                if (save2DbProperty == null || (save2DbProperty.GetValue(item) as bool? ?? true))
                {
                    add.Add(item);
                }
            }
            else
            {
                // Check for changes in DB fields
                var changedFields = fields
                    .Where(field => typeof(T).GetProperty(field) != null)
                    .Where(field =>
                    {
                        var property = typeof(T).GetProperty(field);
                        var newValue = property!.GetValue(item);
                        var oldValue = property!.GetValue(originalItem);
                        return !Equals(newValue, oldValue); // Safer comparison
                    })
                    .ToList();

                if (changedFields.Any())
                {
                    // CRITICAL CHANGE: Use the new UpdateParameterAsync
                    await UpdateParameterAsync(item, uidProperty.GetValue(item) as Guid? ?? Guid.Empty, changedFields);
                }
            }
        });

        await Task.WhenAll(tasksCompare);
    }

    Debug.WriteLine($"{DateTime.Now:HH:mm:ss.ffffff} - Update: Comparison and updates completed.");

    // Delete items
    var tasksDelete = delete.Select(async item =>
    {
        var uid = uidProperty.GetValue(item) as Guid? ?? Guid.Empty;
        // CRITICAL CHANGE: Use DeleteItemAsync (assuming you'll create one)
        await DeleteItemAsync(item, uid); 
    });
    await Task.WhenAll(tasksDelete);
    Debug.WriteLine($"{DateTime.Now:HH:mm:ss.ffffff} - Update: Deleted {delete.Count} items.");

    // Add new items
    var tasksAdd = add.Select(async item =>
    {
        updatedOnProperty.SetValue(item, DateTime.Now);
        updatedByProperty.SetValue(item, updatedBy);
        // CRITICAL CHANGE: Use InsertItemAsync (which you already provided)
        await InsertItemAsync(item); 
    });
    await Task.WhenAll(tasksAdd);
    
    Debug.WriteLine($"{DateTime.Now:HH:mm:ss.ffffff} - Update: Added {add.Count} items.");
    Debug.WriteLine($"{DateTime.Now:HH:mm:ss.ffffff} - Update: Update completed.");
}
        
        public async Task<int> DeleteItemAsync<T>(T item, Guid uid) where T : class
        {
            var tableName = typeof(T).Name;
            var sql = $"DELETE FROM dbo.[{tableName}] WHERE [UID] = @UID";
    
            // Note: ExecuteAsync takes an object for parameters, so using an anonymous object is fine.
            return await ExecuteAsync(sql, new { UID = uid });
        }
        
        
    /// <summary></summary>
    public async Task<List<T>?> LoadData<T, U>(string sql, U parameters)
    {
        
        string connectionString = _configuration.GetConnectionString("DefaultConnection"); 

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Database connection string 'DefaultConnection' not found or is empty in configuration.");
        }
        
        using (IDbConnection connection = new SqlConnection(connectionString))
        {
            try
            {
                var data = await connection.QueryAsync<T>(sql, parameters);
                return data.ToList();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return new List<T>();
            }
        }
    }

    public async Task<int> SaveDataAsync<T>(string sql, T parameters, int? commandTimeout = null)
        => await ExecuteAsync(sql, parameters, commandTimeout);
    
    
    
/// <summary>
/// Updates only the specified fields of an entity.
/// Automatically finds the record using UID first → then falls back to unique fields like Tag, Code, Email, etc.
/// No need to pass UID manually!
/// </summary>
/// <param name="item">The entity with updated values</param>
/// <param name="fields">Fields to update (e.g. "TagDescription", "XEW")</param>
/// <returns>Number of rows affected</returns>
public async Task<int> UpdateParameterAsync<T>(T item, params string[]? fields)
{
    if (item == null) throw new ArgumentNullException(nameof(item));
 if (fields == null || fields.Length == 0) return 0;

 var type = typeof(T);
 var tableName = type.Name;
 var parameters = new DynamicParameters();

 // Build SET clause
 var sets = new List<string>();
 foreach (var field in fields)
 {
     var prop = type.GetProperty(field)
         ?? throw new ArgumentException($"Property '{field}' not found on type {tableName}");

     var value = prop.GetValue(item);
     parameters.Add($"@{field}", value);
     sets.Add($"[{field}] = @{field}");
 }

 // Build smart WHERE clause
 var whereClause = GetUniqueWhereClause(item, parameters, out bool hasUid);

 var sql = $"UPDATE dbo.[{tableName}] SET {string.Join(", ", sets)} WHERE {whereClause}";

 var rowsAffected = await ExecuteAsync(sql, parameters);

 if (rowsAffected == 0 && hasUid)
 {
     _logger.LogWarning("UpdateParameterAsync: No rows updated for {Table} with UID {Uid}", tableName, parameters.Get<Guid>("@Uid"));
 }

 return rowsAffected;
}


/// <summary>
/// Smart helper: finds the best way to identify the record
/// Priority: UID → [Key] attribute → Tag/Code/Name/Email → throws if nothing found
/// </summary>
private string GetUniqueWhereClause<T>(T item, DynamicParameters parameters, out bool hasUid)
{
 hasUid = false;
 var type = typeof(T);

 // 1. Try UID property (most common case-sensitive: UID, Uid, Id, etc.)
 var uidProp = type.GetProperty("UID") ?? type.GetProperty("Uid") ?? type.GetProperty("Id");
 if (uidProp != null && uidProp.PropertyType == typeof(Guid) || uidProp.PropertyType == typeof(Guid?))
 {
     var uidValue = uidProp.GetValue(item);
     if (uidValue is Guid uid && uid != Guid.Empty)
     {
         hasUid = true;
         parameters.Add("@Uid", uid);
         return "[UID] = @Uid";
     }
 }

 // 2. Try property with [Key] attribute
 var keyProp = type.GetProperties()
     .FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null);
 if (keyProp != null)
 {
     var val = keyProp.GetValue(item);
     if (val != null && !string.IsNullOrWhiteSpace(val.ToString()))
     {
         parameters.Add("@Key", val);
         return $"[{keyProp.Name}] = @Key";
     }
 }

 // 3. Fallback: common unique fields (you can customize this list)
 var uniqueCandidates = new[] { "Tag", "Code", "Name", "Email", "UserName", "ProjectCode", "ProjectTag", "LoginId" };

 var conditions = new List<string>();
 foreach (var name in uniqueCandidates)
 {
     var prop = type.GetProperty(name);
     if (prop == null) continue;

     var val = prop.GetValue(item);
     if (val == null || string.IsNullOrWhiteSpace(val.ToString())) continue;

     var paramName = "@" + name;
     parameters.Add(paramName, val);
     conditions.Add($"[{name}] = {paramName}");
 }

 if (conditions.Count > 0)
     return string.Join(" AND ", conditions);

 // 4. Nothing found → fail fast fail
 throw new InvalidOperationException(
     $"Cannot update {type.Name}: No UID and no unique field (Tag/Code/Name/Email) has a value. " +
     "Make sure at least one unique identifier is set on the entity.");
}
    

    /// <summary></summary>
    public async Task SaveData<T>(string sql, T parameters)
    {
        
        string? connectionString = _configuration.GetConnectionString("DefaultConnection"); // Or whatever your connection string name is

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Database connection string 'DefaultConnection' not found or is empty in configuration.");
        }

        using (IDbConnection connection = new SqlConnection(connectionString))
        {
            
            try
            {
                var NoOfRowsAffected = await connection.ExecuteAsync(sql, parameters);
                if (NoOfRowsAffected > 0)
                    Debug.WriteLine($"Saved {NoOfRowsAffected} row(s).");
                else
                    Debug.WriteLine("Save  unsuccessful.");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
            
        }
    }


    /// <summary></summary>
    public async Task DeleteData<T>(string sql)
    {
        string connectionString = _configuration.GetConnectionString("DefaultConnection"); // Or whatever your connection string name is

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Database connection string 'DefaultConnection' not found or is empty in configuration.");
        }

        // Assuming you use Dapper or similar for LoadData
        // If you use Dapper, make sure you have "using Dapper;"
        using (IDbConnection connection = new SqlConnection(connectionString))
        {
            try
            {
                var NoOfRowsAffected = await connection.ExecuteAsync(sql);
                if (NoOfRowsAffected > 0)
                {
                    //System.Diagnostics.Debug.WriteLine($"{NoOfRowsAffected} row(s) deleted.");
                }
                else
                {
                    Debug.WriteLine("Delete  unsuccessful.");
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }
    }

    public async Task<List<string>> GetSqlFieldsNamesAsync(string tableName)
    {
        // The connection string logic is typically handled by how _connection is instantiated.
        // Assuming _connection is a Dapper-ready IDbConnection (like SqlConnection)

        // SQL to get column names from the database
        var query = @"SELECT COLUMN_NAME 
                  FROM INFORMATION_SCHEMA.COLUMNS 
                  WHERE TABLE_NAME = @TableName
                  ORDER BY ORDINAL_POSITION"; // Added ORDER BY for consistency

        // Use Dapper's QueryAsync to execute the query
        var columns = await _connection.QueryAsync<string>(query, new { TableName = tableName });
    
        // Return the results as a List<string>
        return columns.ToList();
    }
    /// <summary></summary>
    public List<string> GetSqlFieldsNames(string tableName)
    {
        string connectionString = _configuration.GetConnectionString("DefaultConnection"); // Or whatever your connection string name is

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Database connection string 'DefaultConnection' not found or is empty in configuration.");
        }

        using SqlConnection connection = new SqlConnection(connectionString);
        var query = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName";
        using var cmd = new SqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@TableName", tableName);
        connection.Open();
        using var reader = cmd.ExecuteReader();
        var dataTable = new DataTable();
        dataTable.Load(reader);
        reader.Close();
        List<string> columns = [];
        foreach (DataRow colRow in dataTable.Rows) columns.Add(colRow.Field<string>("COLUMN_NAME") ?? "");
        return columns;
    }

    /// <summary></summary>
    public async Task<List<T>?> GetList<T>(T item, string selectedProject = "")
    {
        var sql = "select * from dbo." + typeof(T).Name;
        // project filter not applicable for catalogue data or Project tables
        if (typeof(T).Name.Contains("Data") == false 
            && typeof(T).Name.Contains("Project") == false
            && typeof(T).Name.Contains("ProjectUserAssignment") == false
            && typeof(T).Name.Contains("ProjectUserRole") == false) 
            sql += " WHERE ProjectId = '" + selectedProject + "'";
        if (typeof(T).Name == "DBMaster") sql += " OR ProjectId = ''";
        if (typeof(T).Name == "LoadMaster")
            sql = "select * from dbo." + "MasterLoadList" + " WHERE ProjectId = '" + selectedProject + "'";
        
        return await LoadData<T, dynamic>(sql, new { }) ?? [];
    }

    /// <summary></summary>
    public async Task DeleteItem<T>(T item, Guid uid)
    {
        //remove existing entry if any
        await DeleteData<dynamic>("DELETE FROM dbo." + typeof(T).Name + " WHERE  UID = '" + uid + "'");
    }

    /// <summary></summary>
    public async Task DeleteItem1(string dboName, string field, string fieldValue)
    {
        //remove existing entry if any
        await DeleteData<dynamic>("DELETE FROM dbo." + dboName + " WHERE '" + field + "' = '" + fieldValue + "'");
    }


    /// <summary>
    ///     Copy from list and replace in SQL DB
    /// </summary>
    /// <param name="list"></param>
    /// <param name="selectedProject"></param>
    /// <typeparam name="T"></typeparam>
    public async Task BulkCopyDataTableAsync<T>(List<T>? list, String selectedProject = "TestProject")
    {
        foreach (var item in list)
        {
            var propertyInfo = typeof(T).GetProperty("UpdatedOn");
            if (propertyInfo != null && propertyInfo.PropertyType == typeof(DateTime))
            {
                propertyInfo.SetValue(item, DateTime.Now); // Use DateTime.UtcNow for UTC
            }
        }
        
        //remove existing entry if any
        var sql = "DELETE FROM dbo." + typeof(T).Name + " WHERE ProjectId = '" + selectedProject + "'";
        await DeleteData<dynamic>(sql);

        // create a datatable from list as per available SQL columns
        var dataTable = ToDataTable(list);
        
        string connectionString = _configuration.GetConnectionString("DefaultConnection"); // Or whatever your connection string name is

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Database connection string 'DefaultConnection' not found or is empty in configuration.");
        }


        await using SqlConnection connection = new SqlConnection(connectionString);
        await connection.OpenAsync(); // Open connection asynchronously

        using var bulkCopy = new SqlBulkCopy(connection);
        bulkCopy.DestinationTableName = "dbo." + typeof(T).Name;
        bulkCopy.BatchSize = 1000;
        bulkCopy.BulkCopyTimeout = 60;

        foreach (DataColumn column in dataTable.Columns)
            bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);

        try
        {
            await bulkCopy.WriteToServerAsync(dataTable);
            Debug.WriteLine($"Batch copy completed for {dataTable.Rows.Count} rows.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error during bulk copy: {ex.Message}");
            throw;
        }
    }


public async Task Update<T>(List<T> list, List<T> originalList, string updatedBy = "KP")
{
    // Validate properties
    var uidProperty = typeof(T).GetProperty("UID");
    var updatedByProperty = typeof(T).GetProperty("UpdatedBy");
    var updatedOnProperty = typeof(T).GetProperty("UpdatedOn");
    var save2DbProperty = typeof(T).GetProperty("Save2DB");

    if (uidProperty == null || updatedByProperty == null || updatedOnProperty == null)
    {
        throw new ArgumentException("Type T must have UID, UpdatedBy, and UpdatedOn properties.");
    }

    var fields = GetSqlFieldsNames(typeof(T).Name);
    var add = new List<T>();
    var delete = new List<T>();

    // Log start
    Debug.WriteLine($"{DateTime.Now:HH:mm:ss.ffffff} - Update: Starting comparison for {list.Count} items.");

    // Handle empty originalList case
    if (!originalList.Any())
    {
        add.AddRange(list); // All items are new
    }
    else
    {
        // Identify items to delete (in originalList but not in list)
        delete.AddRange(originalList.Where(orig => 
            !list.Any(item => uidProperty.GetValue(item)?.ToString() == uidProperty.GetValue(orig)?.ToString())));

        // Compare and identify items to add or update
        var tasksCompare = list.Select(async item =>
        {
            var uid = uidProperty.GetValue(item)?.ToString();
            var originalItem = originalList.FirstOrDefault(p => uidProperty.GetValue(p)?.ToString() == uid);

            if (originalItem == null)
            {
                // New item
                if (save2DbProperty == null || (save2DbProperty.GetValue(item) as bool? ?? true))
                {
                    add.Add(item);
                }
            }
            else
            {
                // Check for changes in DB fields
                var changedFields = fields
                    .Where(field => typeof(T).GetProperty(field) != null)
                    .Where(field =>
                    {
                        var property = typeof(T).GetProperty(field);
                        var newValue = property!.GetValue(item);
                        var oldValue = property!.GetValue(originalItem);
                        return !Equals(newValue, oldValue); // Safer comparison
                    })
                    .ToList();

                if (changedFields.Any())
                {
                    await UpdateParameter(item, uidProperty.GetValue(item) as Guid? ?? Guid.Empty, changedFields);
                }
            }
        });

        await Task.WhenAll(tasksCompare);
    }

    Debug.WriteLine($"{DateTime.Now:HH:mm:ss.ffffff} - Update: Comparison and updates completed.");

    // Delete items
    var tasksDelete = delete.Select(async item =>
    {
        var uid = uidProperty.GetValue(item) as Guid? ?? Guid.Empty;
        await DeleteItem(item, uid);
    });
    await Task.WhenAll(tasksDelete);
    Debug.WriteLine($"{DateTime.Now:HH:mm:ss.ffffff} - Update: Deleted {delete.Count} items.");

    // Add new items
    var tasksAdd = add.Select(async item =>
    {
        updatedOnProperty.SetValue(item, DateTime.Now);
        updatedByProperty.SetValue(item, updatedBy);
        await InsertItem(item);
    });
    await Task.WhenAll(tasksAdd);
    Debug.WriteLine($"{DateTime.Now:HH:mm:ss.ffffff} - Update: Added {add.Count} items.");
    Debug.WriteLine($"{DateTime.Now:HH:mm:ss.ffffff} - Update: Update completed.");
}
    
    
    /// <summary></summary>
    public async Task UpdateOld16July2025<T>(List<T> list, List<T> originalList, String updatedBy = "KP")
    {
        var UIDProperty = typeof(T).GetProperty("UID");
        if (UIDProperty == null) return;
        var UpdatedByProperty = typeof(T).GetProperty("UpdatedBy");
        var UpdatedOnProperty = typeof(T).GetProperty("UpdatedOn");
        var Save2DBProperty = typeof(T).GetProperty("Save2DB");
        //if (Save2DBProperty == null) return;

        // need to compare only those properties which are present in the DB
        // rest of the properties are derived and therefore need not be compared
        var fields = GetSqlFieldsNames(typeof(T).Name);
        List<T> add = [];
        List<T> delete = [];

        originalList.ForEach(item =>
        {
            var items = list.Where(p => UIDProperty.GetValue(p)?.ToString() == UIDProperty.GetValue(item)?.ToString())
                .ToList();

            if (items.Count == 0)
                // item deleted
                delete.Add(item);
        });
        //
        Debug.WriteLine(
            $"{DateTime.Now:hh.mm.ss.ffffff}- {MethodBase.GetCurrentMethod()?.Name} : {list.Count} compare start");
        var tasksCompare = list.Select(async item =>
        {
            var itemsOriginal = originalList
                .Where(p => UIDProperty.GetValue(p)?.ToString() == UIDProperty.GetValue(item)?.ToString()).ToList();

            if (itemsOriginal.Count == 1)
            {
                // common item
                // check through all the DB fields for any change
                List<string> chagedFields = [];
                fields.ForEach(field =>
                {
                    var property = typeof(T).GetProperty(field);
                    if (property != null)
                    {
                        var itemPropertValue = Convert.ToString(property.GetValue(item));
                        var itemOriginalPropertValue = Convert.ToString(property.GetValue(itemsOriginal[0]));
                        if (itemPropertValue != itemOriginalPropertValue) chagedFields.Add(field);
                    }
                });
                // all fields are checked
                if (chagedFields.Count > 0)
                    await UpdateParameter(item, UIDProperty.GetValue(item) as Guid? ?? Guid.Empty, chagedFields);
            }
            else
            {
                // new item
                // check if this item is programatically created or to be saved to the DB
                if (Save2DBProperty.GetValue(item) as bool? ?? false) add.Add(item);
            }
        });
        // wait till all items are compared and changes are implemented into the DB
        await Task.WhenAll(tasksCompare);
        Debug.WriteLine(
            $"{DateTime.Now:hh.mm.ss.ffffff}- {MethodBase.GetCurrentMethod()?.Name} : {list.Count} compare and update parameters ends");
        //
        var tasksDelete = delete.Select(async item =>
        {
            // var uid = (Guid)UIDProperty!.GetValue(item);
            var uid = UIDProperty.GetValue(item) as Guid? ?? Guid.Empty;
            await DeleteItem(item, uid);
        });
        await Task.WhenAll(tasksDelete);
        // wait till all ietms are deleted
        Debug.WriteLine(
            $"{DateTime.Now:hh.mm.ss.ffffff}- {MethodBase.GetCurrentMethod()?.Name} : {delete.Count} delete ends");
        var tasks2Add = add.Select(async item =>
        {
            SqlParameter dateTimeparameter = new();
            dateTimeparameter.ParameterName = "@Datetime2";
            dateTimeparameter.SqlDbType = SqlDbType.DateTime2;
            dateTimeparameter.Value = DateTime.Parse(DateTime.Now.ToString());
            UpdatedOnProperty!.SetValue(item, dateTimeparameter.Value);

            UpdatedByProperty!.SetValue(item, updatedBy);

            await InsertItem(item);
        });
        // wait till all the new ietms are added
        await Task.WhenAll(tasks2Add);
        Debug.WriteLine(
            $"{DateTime.Now.ToString("hh.mm.ss.ffffff")}- {MethodBase.GetCurrentMethod()?.Name} : {add.Count} added");
        Debug.WriteLine(
            $"{DateTime.Now.ToString("hh.mm.ss.ffffff")}- {MethodBase.GetCurrentMethod()?.Name} : update completed.");

    }


    /// <summary></summary>
    public async Task UpdateItem<T>(T item, Guid uid)
    {
        var SQLFields = GetSqlFieldsNames(typeof(T).Name);
        var sql = "UPDATE dbo." + typeof(T).Name + " SET ";
        SQLFields.ForEach(field => { sql = sql + field + " = @" + field + (field == SQLFields.Last() ? "" : ", "); });
        sql = sql + " WHERE  UID = '" + uid + "'";
        await SaveData(sql, item);
    }

    /// <summary></summary>
    public async Task UpdateParameter<T>(T item, Guid uid, List<string> fields)
    {
        //string sql = "UPDATE dbo." + typeof(T).Name + " SET " + field + " = @" + field + " WHERE  UID = '" + uid + "'";
        var sql = "UPDATE dbo." + typeof(T).Name + " SET ";
        fields.ForEach(field => { sql = sql + field + " = @" + field + (field == fields.Last() ? "" : ", "); });
        sql = sql + " WHERE  UID = '" + uid + "'";
        await SaveData(sql, item);
    }

    /// <summary></summary>
    public async Task UpdateParameterItems<T>(T item, string uidstrings, List<string> fields)
    {
        //string sql = "UPDATE dbo." + typeof(T).Name + " SET " + field + " = @" + field + " WHERE  UID = '" + uid + "'";
        var sql = "UPDATE dbo." + typeof(T).Name + " SET ";
        fields.ForEach(field => { sql = sql + field + " = @" + field + (field == fields.Last() ? "" : ", "); });
        sql = sql + " WHERE  UID IN " + uidstrings;
        await SaveData(sql, item);
    }

    /// <summary></summary>
    public async Task InsertItem<T>(T item)
    {
        //inserting new item
        var SQLFields = GetSqlFieldsNames(typeof(T).Name);
        var sql = "INSERT INTO dbo." + typeof(T).Name + " (" + string.Join(", ", SQLFields) + ") values (@" +
                  string.Join(", @", SQLFields) + "); ";
        await SaveData(sql, item);
    }

    
 public List<T> AssignSequenceToList<T>(List<T> items)
    {
        // Handle null or empty input
        if (items == null || items.Count == 0)
            return items ?? new List<T>();

        // Get the Seq property
        var seqProperty = typeof(T).GetProperty("Seq");
        if (seqProperty == null)
            return items;

        // Ensure Seq property is of integer type
        if (seqProperty.PropertyType != typeof(int) && seqProperty.PropertyType != typeof(int?))
            throw new InvalidOperationException("Seq property must be of type int or int?.");

        // Find the maximum Seq value (excluding 0) to start assigning new sequences
        var defaultSeq = items
            .Select(item => seqProperty.GetValue(item))
            .Where(val => val != null && Convert.ToInt32(val) != 0)
            .DefaultIfEmpty(0)
            .Max(val => Convert.ToInt32(val)) + 1;

        // Assign sequence numbers to items with Seq = 0
        foreach (var item in items)
        {
            var currentSeq = Convert.ToInt32(seqProperty.GetValue(item));
            if (currentSeq == 0)
            {
                seqProperty.SetValue(item, defaultSeq++);
            }
        }

        // Sort the list by Seq property
        return items.OrderBy(item => Convert.ToInt32(seqProperty.GetValue(item))).ToList();
    }

public async Task<(List<T> Items, string Summary)> ImportFromExcel<T>(
    IBrowserFile? file, 
    T item, 
    string? sheetName = null)
{
    var importedItems = new List<T>();
    string summary;

    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    
    if (file == null || file.Size == 0)
    {
        summary = "No file uploaded or file is empty.";
        return (importedItems, summary);
    }

    try
    {
        await using var stream = file.OpenReadStream(file.Size);
        using var package = new ExcelPackage();
        await package.LoadAsync(stream);
        
        // Select worksheet by name or default to first
        ExcelWorksheet? ws;
        if (!string.IsNullOrEmpty(sheetName))
        {
            ws = package.Workbook.Worksheets[sheetName];
            if (ws == null)
            {
                return (importedItems, $"Worksheet '{sheetName}' not found.");
            }
        }
        else
        {
            ws = package.Workbook.Worksheets.FirstOrDefault();
            if (ws == null) return (importedItems, "No worksheet found.");
        }

        var rowCount = ws.Dimension?.Rows ?? 0;
        var colCount = ws.Dimension?.Columns ?? 0;
        
        if (rowCount <= 1 || colCount == 0)
        {
            return (importedItems, $"No data rows found in worksheet '{ws.Name}'.");
        }

        // Map headers to properties (DisplayAttribute preferred)
        var props = typeof(T).GetProperties();
        var headerMap = new Dictionary<int, PropertyInfo>();
        
        for (int c = 1; c <= colCount; c++)
        {
            var header = ws.Cells[1, c].Value?.ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(header)) continue;

            var prop = props.FirstOrDefault(p =>
                (p.GetCustomAttribute<System.ComponentModel.DataAnnotations.DisplayAttribute>()?.Name ?? p.Name)
                    .Equals(header, StringComparison.OrdinalIgnoreCase)
                || p.Name.Equals(header, StringComparison.OrdinalIgnoreCase));

            if (prop != null) headerMap[c] = prop;
        }

        var uidProp = props.FirstOrDefault(p => p.Name == "UID");
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
                        
                        if (targetType == typeof(string)) 
                            val = cell.ToString();
                        else if (targetType == typeof(int)) 
                            val = Convert.ToInt32(cell);
                        else if (targetType == typeof(double)) 
                            val = Convert.ToDouble(cell);
                        else if (targetType == typeof(decimal)) 
                            val = Convert.ToDecimal(cell);
                        else if (targetType == typeof(float)) 
                            val = Convert.ToSingle(cell);
                        else if (targetType == typeof(bool)) 
                            val = Convert.ToBoolean(cell);
                        else if (targetType == typeof(DateTime)) 
                            val = Convert.ToDateTime(cell);
                        else if (targetType == typeof(Guid)) 
                            val = Guid.Parse(cell.ToString()!);
                        else 
                            val = Convert.ChangeType(cell, targetType);

                        tprop.SetValue(instance, val);
                    }
                    catch (Exception convEx)
                    {
                        throw new InvalidOperationException(
                            $"Failed to convert cell [{r},{kv.Key}] ('{cell}') to {tprop.PropertyType.Name}: {convEx.Message}");
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
                _logger.LogWarning(rowEx, "Failed to import row {Row} from sheet {Sheet}", r, ws.Name);
                failedRows.Add($"Row {r}: {rowEx.Message}");
            }
        }

        summary = $"Sheet '{ws.Name}' - Imported: {importedItems.Count}, Failed: {failedRows.Count}";
        if (failedRows.Count > 0)
        {
            summary += ". Failures: " + string.Join("; ", failedRows.Take(5));
            if (failedRows.Count > 5) summary += $"... and {failedRows.Count - 5} more";
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "ImportFromExcel failed for sheet {Sheet}", sheetName ?? "default");
        summary = $"Import failed: {ex.Message}";
    }

    return (importedItems, summary);
}
 
 
    public async Task<(List<T> Items, string Summary)> ImportFromExcel<T>(InputFileChangeEventArgs e, T item, string sheetName = null)
{
    string[] formats =
    [
        "yyyy-MM-ddTHH:mm:ss.fff",
        "MM/dd/yyyy HH:mm:ss",
        "dd/MM/yyyy HH:mm:ss",
        "yyyy/MM/dd HH:mm:ss",
        "MM/dd/yyyy",
        "dd/MM/yyyy",
        "yyyy/MM/dd",
        "yyyy-MM-dd",
        "HH:mm:ss",
        "MM/dd/yyyy hh:mm:ss tt",
        "dd/MM/yyyy hh:mm:ss tt",
        "dd/MM/yyyy hh:mm tt",
        "DD/MM/YYYY HH:MM tt"
    ];
    List<T> importedItems = new List<T>();
    var summary = "";
    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

    try
    {
        if (e.File == null || e.File.Size == 0)
        {
            summary = "Error: No file selected or file is empty.";
            return (importedItems, summary);
        }

        using (var stream = e.File.OpenReadStream(e.File.Size))
        {
            using (var package = new ExcelPackage())
            {
                await package.LoadAsync(stream);
                var worksheet = string.IsNullOrEmpty(sheetName)
                    ? package.Workbook.Worksheets.FirstOrDefault()
                    : package.Workbook.Worksheets[sheetName] 
                      ?? package.Workbook.Worksheets.FirstOrDefault();

                if (worksheet == null)
                {
                    summary = string.IsNullOrEmpty(sheetName)
                        ? "Error: No worksheet found in the Excel file."
                        : $"Error: Worksheet '{sheetName}' not found.";
                    return (importedItems, summary);
                }

                if (worksheet == null)
                {
                    summary = "Error: No worksheet found in the Excel file.";
                    return (importedItems, summary);
                }

                var rowCount = worksheet.Dimension?.Rows ?? 0;
                var colCount = worksheet.Dimension?.Columns ?? 0;

                if (rowCount <= 1 || colCount == 0)
                {
                    summary = "Error: Excel file has no data or only headers.";
                    return (importedItems, summary);
                }

                // Map headers to properties and their column indices
                Dictionary<string, (PropertyInfo Property, int ColumnIndex)> headerMap = new Dictionary<string, (PropertyInfo, int)>();
                var properties = typeof(T).GetProperties();
                for (var col = 1; col <= colCount; col++)
                {
                    var header = worksheet.Cells[1, col].Value?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(header))
                    {
                        var property = properties.FirstOrDefault(p =>
                            (p.GetCustomAttribute<DisplayAttribute>()?.Name ?? p.Name) == header);
                        if (property != null)
                        {
                            headerMap[header] = (property, col);
                        }
                    }
                }

                // Find UID property and its header
                var uidProperty = properties.FirstOrDefault(p => p.Name == "UID");
                string uidHeader = uidProperty?.GetCustomAttribute<DisplayAttribute>()?.Name ?? "UID";

                var successfulImports = 0;
                var failedImports = 0;
                List<string> failedRows = new List<string>();
                var processedUIDs = new HashSet<object>();

                for (var row = 2; row <= rowCount; row++)
                {
                    var newItem = Activator.CreateInstance<T>()!;
                    var rowFailed = false;

                    // Populate properties from Excel data
                    foreach (var kvp in headerMap)
                    {
                        var col = kvp.Value.ColumnIndex; // Use the stored column index
                        var cellValue = worksheet.Cells[row, col].Value;

                        try
                        {
                            if (cellValue != null)
                            {
                                var propertyType = kvp.Value.Property.PropertyType;
                                var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

                                if (underlyingType == typeof(string))
                                {
                                    kvp.Value.Property.SetValue(newItem, cellValue.ToString());
                                }
                                else if (underlyingType == typeof(int) &&
                                         int.TryParse(cellValue.ToString(), out var intValue))
                                {
                                    kvp.Value.Property.SetValue(newItem, intValue);
                                }
                                else if (underlyingType == typeof(double) &&
                                         double.TryParse(cellValue.ToString(), out var doubleValue))
                                {
                                    kvp.Value.Property.SetValue(newItem, doubleValue);
                                }
                                else if (underlyingType == typeof(float) &&
                                         float.TryParse(cellValue.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var floatValue))
                                {
                                    kvp.Value.Property.SetValue(newItem, floatValue);
                                }
                                else if (underlyingType == typeof(DateTime) &&
                                         DateTime.TryParseExact(cellValue.ToString(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateValue))
                                {
                                    kvp.Value.Property.SetValue(newItem, dateValue);
                                }
                                else if (underlyingType == typeof(bool) &&
                                         bool.TryParse(cellValue.ToString(), out var boolValue))
                                {
                                    kvp.Value.Property.SetValue(newItem, boolValue);
                                }
                                else if (underlyingType == typeof(decimal) &&
                                         decimal.TryParse(cellValue.ToString(), out var decimalValue))
                                {
                                    kvp.Value.Property.SetValue(newItem, decimalValue);
                                }
                                else if (underlyingType == typeof(long) &&
                                         long.TryParse(cellValue.ToString(), out var longValue))
                                {
                                    kvp.Value.Property.SetValue(newItem, longValue);
                                }
                                else if (underlyingType == typeof(Guid) &&
                                         Guid.TryParse(cellValue.ToString(), out var guidValue))
                                {
                                    kvp.Value.Property.SetValue(newItem, guidValue);
                                }
                            }
                            else
                            {
                                if (Nullable.GetUnderlyingType(kvp.Value.Property.PropertyType) != null)
                                    kvp.Value.Property.SetValue(newItem, null);
                            }
                        }
                        catch (Exception)
                        {
                            rowFailed = true;
                            failedRows.Add($"Row {row}: Invalid data format for column '{kvp.Key}'.");
                            break;
                        }
                    }

                    // Handle UID (generate if missing, validate if present)
                    if (uidProperty != null)
                    {
                        var uidValue = uidProperty.GetValue(newItem);

                        // If UID is not in the Excel file, generate a new one
                        if (!headerMap.ContainsKey(uidHeader))
                        {
                            uidValue = Guid.NewGuid();
                            uidProperty.SetValue(newItem, uidValue);
                        }

                        // Validate UID
                        if (uidValue == null || uidValue.Equals(Guid.Empty))
                        {
                            rowFailed = true;
                            failedRows.Add($"Row {row}: UID is null or empty.");
                        }
                        else if (!processedUIDs.Add(uidValue))
                        {
                            rowFailed = true;
                            failedRows.Add($"Row {row}: Duplicate UID '{uidValue}'.");
                        }
                    }
                    else
                    {
                        // If UID property is not found in the class, fail the row
                        rowFailed = true;
                        failedRows.Add($"Row {row}: UID property not found in class definition.");
                    }

                    if (!rowFailed)
                    {
                        importedItems.Add(newItem);
                        successfulImports++;
                    }
                    else
                    {
                        failedImports++;
                    }
                }

                summary =
                    $"Import Summary: Successful Imports: {successfulImports}, Failed Imports: {failedImports}.";
                if (failedImports > 0)
                    summary += $" Failed Rows: {string.Join(", ", failedRows)}.";
            }
        }
    }
    catch (Exception ex)
    {
        summary = $"An error occurred during import: {ex.Message}";
    }

    return (importedItems, summary);
}
    
    
  
    public byte[] GenerateExcelWorkbookByte<T>(List<T>? list)
    {
        if (list == null || list.Count == 0)
        {
            list = [];
            var newItem = Activator.CreateInstance<T>();
            list.Add(newItem);
        }

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        var stream = new MemoryStream();

        using var package = new ExcelPackage(stream);
        var ws = package.Workbook.Worksheets.Add(typeof(T).Name);

        // Get properties to include (exclude those with ExcludeFromExcelExport attribute)
        var properties = typeof(T).GetProperties()
            .Where(p => !p.GetCustomAttributes(typeof(ExcludeFromExcelExportAttribute), false).Any())
            .OrderBy(p => p.GetCustomAttribute<DisplayAttribute>()?.Order ?? int.MaxValue)
            .ToList();

        // Manually create the header row using Display names
        for (int col = 0; col < properties.Count; col++)
        {
            var displayName = properties[col].GetCustomAttribute<DisplayAttribute>()?.Name ?? properties[col].Name;
            ws.Cells[1, col + 1].Value = displayName;
        }

        // Populate data rows
        for (int row = 0; row < list.Count; row++)
        {
            for (int col = 0; col < properties.Count; col++)
            {
                var value = properties[col].GetValue(list[row]);
                ws.Cells[row + 2, col + 1].Value = value;
            }
        }

        // Apply formatting
        var range = ws.Cells[1, 1, list.Count + 1, properties.Count];
        range.AutoFilter = true;
        range.AutoFitColumns();

        // Limit column width
        for (var col = 1; col <= properties.Count; col++)
        {
            ws.Column(col).Width = Math.Min(ws.Column(col).Width, 100); // max width set as 100
        }

        ws.Row(1).Style.Font.Bold = true;
        ws.View.FreezePanes(2, 5);

        return package.GetAsByteArray();
    }
    
    
    
    /// <summary></summary>
    public byte[] GenerateExcelWorkbookByteOld16July2025<T>(List<T>? list)
    {
        if (list == null || list.Count == 0)
        {
            list = [];
            // T newItem = (T)Activator.CreateInstance(typeof(T));
            var newItem = Activator.CreateInstance<T>();
            list.Add(newItem);
        }

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        var stream = new MemoryStream();

        using var package = new ExcelPackage(stream);

        var ws = package.Workbook.Worksheets.Add(typeof(T).Name);

        var range = ws.Cells["A1"].LoadFromCollection(list, true);
        range.AutoFilter = true;
        range.AutoFitColumns();
        // Iterate through each column
        for (var col = 1; col <= ws.Dimension.Columns; col++)
            // Get the maximum width for the column
            //double maxWidth = ws.Cells[1, col, ws.Dimension.Rows, col].Max(cell => cell.Value.ToString().Length);
            // Set the column width
            ws.Column(col).Width = Math.Min(ws.Column(col).Width, 100); // max width set as 100
        ws.Row(1).Style.Font.Bold = true;

        ws.View.FreezePanes(2, 5);
        return package.GetAsByteArray();
    }


    /// <summary></summary>
    public async Task ExportExcelList<T>(List<T> items)
    {
        try
        {
            var file = new FileInfo(@"E:\Dev\Data\Output\" + typeof(T).Name + "s.xlsx");
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
            // Action after the exception is caught
            Debug.WriteLine("Export Error : " + ex.Message);
        }
    }


    /// <summary>
    ///     Create DataTable as per given List however as per corresponding SQL DB columns
    /// </summary>
    /// <param name="data"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public DataTable ToDataTable<T>(List<T>? data)
    {
        var table = new DataTable(typeof(T).Name);
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        List<string> filedNames = GetSqlFieldsNames(typeof(T).Name);
        foreach (var prop in properties)
            if (filedNames.Contains(prop.Name))
            {
                var propType = prop.PropertyType;

                // Handle nullable types
                if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    propType = Nullable.GetUnderlyingType(propType);

                table.Columns.Add(prop.Name, propType);
            }

        foreach (var item in data)
        {
            var row = table.NewRow();
            foreach (var prop in properties)
                if (filedNames.Contains(prop.Name))
                    row[prop.Name] = prop.GetValue(item) ?? DBNull.Value; // Handle null values

            table.Rows.Add(row);
        }

        return table;
    }
    
    
    public async Task<List<string>> GetTablesAsync(string connectionString)
    {
        var tables = new List<string>();

        await using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();
            var command =
                new SqlCommand("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'",
                    connection);
            var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync()) tables.Add(reader.GetString(0));
        }

        return tables;
    }

    public async Task CopyTablesAsync(string sourceConnectionString, string destinationConnectionString,
        List<string> tablesToCopy)
    {
        foreach (var table in tablesToCopy)
        {
            try
            {
                await using (var sourceConnection = new SqlConnection(sourceConnectionString))
                await using (var destinationConnection = new SqlConnection(destinationConnectionString))
                {
                    await sourceConnection.OpenAsync();
                    await destinationConnection.OpenAsync();

                    // Step 1: Get the schema of the source table
                    var schemaTable = await GetTableSchemaAsync(sourceConnection, table);

                    // Step 2: Drop the destination table if it exists
                    await DropTableIfExistsAsync(destinationConnection, table);

                    // Step 3: Create the destination table with the same schema as the source table
                    await CreateTableAsync(destinationConnection, table, schemaTable);

                    // Step 4: Copy the data from the source table to the destination table
                    await CopyDataAsync(sourceConnection, destinationConnection, table);
                }
            }
            catch (Exception ex)
            {
                var logErrorText = $"Error processing table '{table}': {ex.Message}";
                _logger.LogError(logErrorText, ex);
                // do not throw Exception, only log error. 
                //throw new Exception(logErrorText, ex);
            }
        }
    }

    public async Task<DataTable> GetTableSchemaAsync(SqlConnection connection, string tableName)
    {
        var schemaQuery = $@"
        SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE
        FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_NAME = @tableName
        ORDER BY ORDINAL_POSITION";

        using (var command = new SqlCommand(schemaQuery, connection))
        {
            command.Parameters.AddWithValue("@tableName", tableName);
            using (var reader = await command.ExecuteReaderAsync())
            {
                var schemaTable = new DataTable();
                schemaTable.Load(reader);
                return schemaTable;
            }
        }
    }


    public async Task CreateTableAsync(SqlConnection connection, string tableName, DataTable schemaTable)
    {
        var columns = new List<string>();

        foreach (DataRow row in schemaTable.Rows)
        {
            var columnName = row["COLUMN_NAME"].ToString();
            var dataType = row["DATA_TYPE"].ToString();
            var isNullable = row["IS_NULLABLE"].ToString();
            var maxLength = row["CHARACTER_MAXIMUM_LENGTH"] as int?;

            var columnDefinition = $"{columnName} {dataType}";

            // Handle length for string types (e.g., VARCHAR, NVARCHAR)
            if (maxLength.HasValue)
            {
                if (maxLength == -1)
                    // Use MAX for columns with unlimited length
                    columnDefinition += "(MAX)";
                else if (maxLength > 0 && (dataType == "varchar" || dataType == "nvarchar" || dataType == "char" ||
                                           dataType == "nchar"))
                    // Use the specified length for other string types
                    columnDefinition += $"({maxLength})";
            }

            // Handle precision and scale for decimal/numeric types
            if (dataType == "decimal" || dataType == "numeric")
            {
                var numericPrecision = row["NUMERIC_PRECISION"] as byte?;
                var numericScale = row["NUMERIC_SCALE"] as byte?;
                if (numericPrecision.HasValue && numericScale.HasValue)
                    columnDefinition += $"({numericPrecision}, {numericScale})";
            }

            // Handle nullability
            if (isNullable == "NO") columnDefinition += " NOT NULL";

            columns.Add(columnDefinition);
        }

        var createTableQuery = $@"
        CREATE TABLE {tableName} (
            {string.Join(", ", columns)}
        )";

        using (var command = new SqlCommand(createTableQuery, connection))
        {
            await command.ExecuteNonQueryAsync();
        }
    }


    public async Task DropTableIfExistsAsync(SqlConnection connection, string tableName)
    {
        var dropTableQuery = $@"
        IF OBJECT_ID('{tableName}', 'U') IS NOT NULL
        BEGIN
            DROP TABLE {tableName};
        END";

        using (var command = new SqlCommand(dropTableQuery, connection))
        {
            await command.ExecuteNonQueryAsync();
        }
    }


    public async Task CopyDataAsync(SqlConnection sourceConnection, SqlConnection destinationConnection,
        string tableName)
    {
        var selectCommand = new SqlCommand($"SELECT * FROM {tableName}", sourceConnection);
        var reader = await selectCommand.ExecuteReaderAsync();

        using (var bulkCopy = new SqlBulkCopy(destinationConnection))
        {
            bulkCopy.DestinationTableName = tableName;
            await bulkCopy.WriteToServerAsync(reader);
        }
    }
    
        
        
        
    }
