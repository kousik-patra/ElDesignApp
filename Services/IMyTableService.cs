using System;
using System.IO;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ElDesignApp.Services;


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
using Dapper;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearRegression;
using Microsoft.AspNetCore.Components.Forms;
using OfficeOpenXml;

public interface IMyTableService
{
       /// <summary></summary>
        Task<List<T>?> GetList<T>(T item, string selectedProject= "");
        /// <summary></summary>
        Task<List<T>?> LoadData<T, U>(string sql, U parameters);

        /// <summary></summary>
        Task SaveData<T>(string sql, T parameters);

        /// <summary>
        ///     Bulk Copy by deleting the existing record and refreshing as per List T
        /// </summary>
        /// <param name="list"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        Task BulkCopyDataTableAsync<T>(List<T>? list, String selectedProject = "TestProject");

        /// <summary></summary>
        Task DeleteData<T>(string sql);

        /// <summary></summary>
        Task Update<T>(List<T> list, List<T> originalList, String updatedBy = "KP");

        /// <summary></summary>
        Task UpdateItem<T>(T item, Guid uid);

        /// <summary></summary>
        Task UpdateParameter<T>(T item, Guid uid, List<string> fields);

        /// <summary></summary>
        Task UpdateParameterItems<T>(T item, string uidstrings, List<string> fields);

        /// <summary></summary>
        List<T> AssignSequenceToList<T>(List<T> items);

        /// <summary>
        ///     import list of item type T from excel file e with headers matching the properties of type T
        /// </summary>
        /// <param name="e"></param>
        /// <param name="item"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        Task<(List<T> Items, string Summary)> ImportFromExcel<T>(InputFileChangeEventArgs e, T item);

        /// <summary></summary>
        Task InsertItem<T>(T item);

        /// <summary></summary>
        Task DeleteItem<T>(T item, Guid uid);

        /// <summary></summary>
        byte[] GenerateExcelWorkbookByte<T>(List<T>? list);

        /// <summary></summary>
        Task ExportExcelList<T>(List<T> items);

        /// <summary></summary>
        Task DeleteItem1(string dboName, string field, string fieldValue);

        /// <summary></summary>
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
    /// <summary></summary>
    public SqlConnectionConfiguration(string connectionString)
    {
        ConnectionString = connectionString;
    }

    /// <summary></summary>
    public string ConnectionString { get; }
}


public class MyTableService : IMyTableService
    {
        
        private readonly IConfiguration _configuration;
        private readonly ICacheService _cache;
        private readonly IGlobalDataService _globalData;
        private readonly ILogger<DataRetrievalService> _logger;
        
        // Constructor for dependency injection
        public MyTableService(
            IGlobalDataService globalData,
            IConfiguration configuration,
            ICacheService cache,
            ILogger<DataRetrievalService> logger
            )
        {
            _cache = cache;
            _globalData = globalData;
            _configuration = configuration;
            GetConnectionString();
            _logger = logger;
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
        if (typeof(T).Name.Contains("Data") == false && typeof(T).Name.Contains("Project") == false ) 
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

/// <summary>
/// Updates the database by comparing a new list with the original list.
/// Adds new items, updates changed items, and deletes removed items.
/// </summary>
/// <param name="list">New list of items.</param>
/// <param name="originalList">Original list from the database.</param>
/// <param name="updatedBy">User performing the update (default: "KP").</param>
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


    /// <summary>
    /// </summary>
    /// <param name="items"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
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


    public async Task<(List<T> Items, string Summary)> ImportFromExcel<T>(InputFileChangeEventArgs e, T item)
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
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();

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
    
    
    /// <summary>
    ///     import list of item type T from excel file e with headers matching the properties of type T
    /// </summary>
    /// <param name="e"></param>
    /// <param name="item"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public async Task<(List<T> Items, string Summary)> ImportFromExcelOld15July2025<T>(InputFileChangeEventArgs e, T item)
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
                    await package.LoadAsync(stream); // Use LoadAsync
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();

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

                    Dictionary<string, int> headerMap = new Dictionary<string, int>();
                    for (var col = 1; col <= colCount; col++)
                    {
                        var header = worksheet.Cells[1, col].Value?.ToString();
                        if (!string.IsNullOrEmpty(header)) headerMap[header.Trim()] = col;
                    }

                    Dictionary<string, PropertyInfo> propertyMap = typeof(T).GetProperties()
                        .ToDictionary(p => p.Name.Trim(), p => p);

                    var successfulImports = 0;
                    var failedImports = 0;
                    List<string> failedRows = new List<string>();
                    var processedUIDs = new HashSet<object>();

                    var uidProperty = propertyMap.GetValueOrDefault("UID");

                    for (var row = 2; row <= rowCount; row++)
                    {
                        var newItem = Activator.CreateInstance<T>()!;
                        var rowFailed = false;

                        foreach (var kvp in propertyMap)
                            if (headerMap.TryGetValue(kvp.Key, out var col))
                            {
                                var cellValue = worksheet.Cells[row, col].Value;

                                try
                                {
                                    if (cellValue != null)
                                    {
                                        var propertyType = kvp.Value.PropertyType;
                                        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

                                        
                                        if (underlyingType == typeof(string))
                                        {
                                            kvp.Value.SetValue(newItem, cellValue.ToString());
                                        }
                                        else if (underlyingType == typeof(int) &&
                                                 int.TryParse(cellValue.ToString(), out var intValue))
                                        {
                                            kvp.Value.SetValue(newItem, intValue);
                                        }
                                        else if (underlyingType == typeof(double) &&
                                                 double.TryParse(cellValue.ToString(), out var doubleValue))
                                        {
                                            kvp.Value.SetValue(newItem, doubleValue);
                                        }
                                        
                                        else if (underlyingType == typeof(float) )
                                        {
                                            // Convert cellValue to string and handle cultural differences
                                            string cellString = cellValue.ToString().Trim();
                                            if (float.TryParse(cellString, NumberStyles.Any, CultureInfo.InvariantCulture, out var floatValue))
                                            {
                                                kvp.Value.SetValue(newItem, floatValue);
                                            }
                                            else
                                            {
                                                // Log or handle invalid float values (optional)
                                                Debug.WriteLine($"Failed to parse '{cellString}' as float for property '{kvp.Key}'");
                                            }
                                        }
                                        
                                        
                                        
                                        // else if (underlyingType == typeof(float) &&
                                        //          float.TryParse(cellValue.ToString(), out var floatValue))
                                        // {
                                        //     kvp.Value.SetValue(newItem, floatValue);
                                        // }


                                        else if (underlyingType == typeof(DateTime) )
                                                 // && DateTime.TryParse(cellValue.ToString(), out DateTime dateValue))
                                        {
                                            kvp.Value.SetValue(newItem, cellValue);
                                        }

                                        else if (underlyingType == typeof(bool) &&
                                                 bool.TryParse(cellValue.ToString(), out var boolValue))
                                        {
                                            kvp.Value.SetValue(newItem, boolValue);
                                        }
                                        else if (underlyingType == typeof(decimal) &&
                                                 decimal.TryParse(cellValue.ToString(), out var decimalValue))
                                        {
                                            kvp.Value.SetValue(newItem, decimalValue);
                                        }
                                        else if (underlyingType == typeof(long) &&
                                                 long.TryParse(cellValue.ToString(), out var longValue))
                                        {
                                            kvp.Value.SetValue(newItem, longValue);
                                        }
                                        else if (underlyingType == typeof(Guid) &&
                                                 Guid.TryParse(cellValue.ToString(), out var guidValue))
                                        {
                                            kvp.Value.SetValue(newItem, guidValue);
                                        }
                                    }
                                    else
                                    {
                                        if (Nullable.GetUnderlyingType(kvp.Value.PropertyType) != null)
                                            kvp.Value.SetValue(newItem, null);
                                    }
                                }
                                catch (Exception)
                                {
                                    rowFailed = true;
                                    break;
                                }
                            }

                        if (uidProperty != null)
                        {
                            var uidValue = uidProperty.GetValue(newItem);

                            if (uidValue == null)
                            {
                                if (Nullable.GetUnderlyingType(uidProperty.PropertyType) == null)
                                {
                                    rowFailed = true;
                                    failedRows.Add($"Row {row}: UID is null.");
                                }
                            }
                            else if (!processedUIDs.Add(uidValue))
                            {
                                rowFailed = true;
                                failedRows.Add($"Row {row}: Duplicate UID '{uidValue}'.");
                            }
                        }

                        if (rowFailed)
                        {
                            failedImports++;
                        }
                        else
                        {
                            importedItems.Add(newItem);
                            successfulImports++;
                        }
                    }

                    summary =
                        $"Import Summary: Successful Imports: {successfulImports}, Failed Imports: {failedImports}.";
                    if (failedImports > 0) summary += $" Failed Rows: {string.Join(", ", failedRows)}.";
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
