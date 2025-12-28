using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SixLabors.ImageSharp;

namespace ElDesignApp.Services;

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
using OfficeOpenXml;
using ElDesignApp.Services;


public interface IMyFunctionService
{
    double[] Polynomial(double[] x, double[] y, int order);


    Tuple<T, List<string>> ValidateGeneralInput<T>(T itemChanged, PropertyInfo? propertyInfo,
        List<T> changedList, List<T> list);

    string LogMessage(string logInfo, string logWarning, string logError);


}


public class MyFunctionService(IGlobalDataService globalData) : IMyFunctionService
{
    private readonly IGlobalDataService _globalData = globalData; 

    // Inject IGlobalDataService into the constructor


    public double[] Polynomial(double[] x, double[] y, int order)
    {
        var design = Matrix<double>.Build.Dense(x.Length, order + 1, (i, j) => Math.Pow(x[i], j));
        return MultipleRegression.QR(design, MathNet.Numerics.LinearAlgebra.Vector<double>.Build.Dense(y)).ToArray();
    }
    
    /// <summary></summary>

    
    private static PropertyInfo GetPropertyOrThrow<T>(string propertyName)
    {
        return typeof(T).GetProperty(propertyName)
               ?? throw new InvalidOperationException($"Property '{propertyName}' not found in type {typeof(T).Name}.");

    }
public Tuple<T, List<string>> ValidateGeneralInput<T>(T itemChanged, PropertyInfo? propertyInfo,
    List<T> changedList, List<T> list)
{
    var infoMessage = "";
    var successMessage = "";
    var warningMessage = "";
    var errorMessage = "";

    var propertyName = propertyInfo?.Name;

    var uidProperty = GetPropertyOrThrow<T>("UID");
    
    // Ensure inputs are valid
    if (itemChanged == null || uidProperty == null || propertyInfo == null || list == null)
    {
        throw new ArgumentNullException("One or more required inputs (itemChanged, uidProperty, propertyInfo, or list) are null.");
    }

    // Get the UID from the itemChanged (assuming uidProperty returns a Guid)
    if (uidProperty.GetValue(itemChanged) is not Guid uid)
    {
        throw new InvalidOperationException("The uidProperty does not return a Guid value or is null.");
    }

    // Find the matching item in the list (using direct Guid comparison)
    var itemExisting = list.FirstOrDefault(item => Equals(uidProperty.GetValue(item), uid))
                       ?? throw new InvalidOperationException($"No item found in the list with UID: {uid}");

    // Get the property values from the existing and changed items
    var existingValue = propertyInfo.GetValue(itemExisting);
    var changedValue = propertyInfo.GetValue(itemChanged);

    // Try checking custom validation, if any
    try
    {
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(itemChanged, null, null);
        var isValidCustom = Validator.TryValidateObject(itemChanged, context, validationResults, true);

        if (!isValidCustom)
        {
            foreach (var validationResult in validationResults)
            {
                Debug.WriteLine($"{propertyName}: {validationResult.ErrorMessage}");
                errorMessage += validationResult.ErrorMessage + " ";
            }
            
            // Revert to original value on custom validation failure
            if (!string.IsNullOrEmpty(errorMessage))
            {
                propertyInfo.SetValue(itemChanged, existingValue);
                errorMessage += $"Reverting to the existing value '{existingValue}'.";
            }
        }
        else
        {
            successMessage += "Success: Custom rules validated. ";
        }
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"Error: {propertyInfo.Name}. Custom validation {ex}.");
        errorMessage += $"Custom validation error. ";
    }

    // Check Regular Expression Validity (only if no errors so far)
    if (string.IsNullOrEmpty(errorMessage))
    {
        try
        {
            // Get the RegularExpressionAttribute if it exists
            var regexAttribute = propertyInfo.GetCustomAttributes(typeof(RegularExpressionAttribute), false)
                .FirstOrDefault() as RegularExpressionAttribute;
            
            if (regexAttribute != null)
            {
                // Get the value of the property
                var value = propertyInfo.GetValue(itemChanged);
                
                // Check if value is null or empty
                if (value == null || string.IsNullOrEmpty(value.ToString()))
                {
                    // Revert to original value instead of throwing
                    propertyInfo.SetValue(itemChanged, existingValue);
                    infoMessage += $"Empty value not allowed for {propertyName}. Reverted to existing value '{existingValue}'. ";
                }
                else
                {
                    var isValid = regexAttribute.IsValid(value);
                    if (isValid)
                    {
                        successMessage += "Success: Entered data is validated. Press update to save the changes to database. ";
                    }
                    else
                    {
                        // Revert to original value
                        propertyInfo.SetValue(itemChanged, existingValue);
                        errorMessage += $"{regexAttribute.ErrorMessage} Reverting to the existing value '{existingValue}'. ";
                    }
                }
            }
            else
            {
                // No regex validation defined - this is OK
                Debug.WriteLine($"No RegularExpression validation defined for {propertyName}");
            }
        }
        catch (Exception ex)
        {
            // Revert to original value on any regex validation error
            propertyInfo.SetValue(itemChanged, existingValue);
            errorMessage += $"Validation error: {ex.Message}. Reverting to the existing value '{existingValue}'. ";
        }
    }

    // Check for duplicate Tag (only if no errors so far)
    if (string.IsNullOrEmpty(errorMessage) && propertyName == "Tag")
    {
        try
        {
            var tagProperty = GetPropertyOrThrow<T>("Tag");
            
            // Check for empty tag
            if (changedValue == null || string.IsNullOrWhiteSpace(changedValue.ToString()))
            {
                // Revert to original value
                propertyInfo.SetValue(itemChanged, existingValue);
                errorMessage += $"Tag cannot be empty. Reverting to the existing tag '{existingValue}'. ";
            }
            else
            {
                // Check for duplicate tag
                var tagCount = changedList?.Count(a => 
                    tagProperty.GetValue(a)?.ToString()?.Equals(changedValue.ToString(), StringComparison.OrdinalIgnoreCase) == true);
                
                if (tagCount > 1)
                {
                    // Duplicate Tag - revert to original value
                    propertyInfo.SetValue(itemChanged, existingValue);
                    errorMessage += $"Entered tag '{changedValue}' already exists. Reverting to the existing tag '{existingValue}'. ";
                }
                else
                {
                    successMessage += $"Tag '{changedValue}' is valid. ";
                }
            }
        }
        catch (Exception ex)
        {
            // Revert to original value on any tag validation error
            propertyInfo.SetValue(itemChanged, existingValue);
            errorMessage += $"Tag validation error: {ex.Message}. Reverting to the existing tag '{existingValue}'. ";
        }
    }

    Debug.WriteLine($"Validation for '{propertyInfo.Name}' complete.");
    var messages = new List<string> { infoMessage, successMessage, warningMessage, errorMessage };
    return new Tuple<T, List<string>>(itemChanged, messages);
}


    /// <summary>
    /// this function return the Log Message ignoring empty values
    /// </summary>
    /// <param name="logInfo"></param>
    /// <param name="logWarning"></param>
    /// <param name="logError"></param>
    /// <returns></returns>
    public string LogMessage(string logInfo, string logWarning, string logError)
    {
        var str = string.Join(", ", 
            new[] 
            { 
                !string.IsNullOrEmpty(logInfo) && logInfo != "none" ? $"LogInfo: {logInfo}" : null,
                !string.IsNullOrEmpty(logWarning) && logWarning != "none" ? $"LogWarning: {logWarning}" : null,
                !string.IsNullOrEmpty(logError) && logError != "none" ? $"LogError: {logError}" : null
            }.Where(s => s != null));
        return $"{DateTime.Now:hh.mm.ss.ffffff} : {str}";
    }
    
    

    
    
    
    
}