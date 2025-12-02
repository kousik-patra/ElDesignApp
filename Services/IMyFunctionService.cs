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

    Task<PlotPlan> LoadImage(InputFileChangeEventArgs e);

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
        //
        //System.Diagnostics.Debug.WriteLine($"{DateTime.Now.ToString("hh.mm.ss.ffffff")} - {MethodBase.GetCurrentMethod()?.Name}: " +
        //       $" Item Tag '{existingTag}' :  Property '{propertyName}' of type '{propertyType}': being changed from '{existingValue}' to '{changedValue}'");
        //


        // try checking custom validation, if any

        try
        {
            var validationResults = new List<ValidationResult>();

            var context = new ValidationContext(itemChanged, null, null);

            var isValidCustom = Validator.TryValidateObject(itemChanged, context, validationResults, true);

            if (!isValidCustom)
                foreach (var validationResult in validationResults)
                {
                    Debug.WriteLine($"{propertyName}: {validationResult.ErrorMessage}");
                    errorMessage += validationResult.ErrorMessage;
                }
            else
                //System.Diagnostics.Debug.WriteLine($"{typeof(T)} Custom rules validated.");
                successMessage += "Success: Custom rules validated.";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error: {propertyInfo.Name}. Custom validation {ex}.");
        }


        // check Regular Expression Validity
        try
        {
            // Get the RegularExpressionAttribute if it exists
            var regexAttribute = propertyInfo.GetCustomAttributes(typeof(RegularExpressionAttribute), false)
                .FirstOrDefault() as RegularExpressionAttribute;
            if (regexAttribute == null)
                throw new InvalidOperationException(
                    $"Property {propertyName} does not have a RegularExpressionAttribute");
            // Get the value of the property
            var value = propertyInfo.GetValue(itemChanged);
            //
            if (value == null || string.IsNullOrEmpty(value.ToString()))
                throw new ArgumentException($"Value of the property {propertyName} is null or not a string");

            //
            var isValid = regexAttribute.IsValid(value);
            if (isValid)
                successMessage += "Success: Entered data is validated. Press update to save the changes to database.";
            else
                errorMessage +=
                    $"Error Message: {regexAttribute.ErrorMessage}. Revertng to the existing value '{existingValue}'.";
        }
        catch (InvalidOperationException ex)
        {
            warningMessage +=
                $"Warning: Validation rule not available. Changed value '{changedValue}' could not be validated";
        }
        catch (ArgumentException ex)
        {
            infoMessage += $"Information: {ex}. Changed value '{changedValue}' could not be validated";
        }
        catch (Exception ex)
        {
            errorMessage += $"Error: {ex}. Revertng to the existing value";
        }


        //
        if (errorMessage == "" && propertyName == "Tag")
        {
            // further, if PropertyInfo Name is 'Tag' then check for duplicate tag
            
            var tagProperty = GetPropertyOrThrow<T>("Tag");
            
            var tagCount = changedList?.Where(a => tagProperty.GetValue(a).ToString() == changedValue.ToString())
                .ToList().Count;
            if (tagCount > 1)
                // duplicate Tag
                errorMessage +=
                    $"Entered tag '{changedValue}' already exists. Revertng to the existing tag '{existingValue}'";
            else if (string.IsNullOrEmpty(changedValue.ToString()))
                // empty Tag value
                errorMessage += $"Entered empty value. Revertng to the existing tag '{existingValue}'";
            //successMessage += $"Existing Tag '{existingValue}' changed to tag '{changedValue}'. Press update to save the changes to database.";

            if (errorMessage != "")
            {
                // revert to original item (tag)
                tagProperty.SetValue(itemChanged, existingValue);
                successMessage = "";
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
    
    
    public async Task<PlotPlan> LoadImage(InputFileChangeEventArgs e)
{
    try
    {
        PlotPlan newImage = new()
        {
            UID = Guid.NewGuid()
        };
        const int maxFileMBSize = 80;
        const long maxFileSize = maxFileMBSize * 1024 * 1024; // 80MB max file size
        const int maxOriginalWidth = 192000; // Reasonable max width for original
        const int maxOriginalHeight = 108000; // Reasonable max height for original
        const int thumbnailWidth = 400;
        const int thumbnailHeight = 250;

        // Validate file type
        string format = e.File.ContentType.ToLower() switch
        {
            "image/png" => "image/png",
            "image/jpeg" => "image/jpeg",
            _ => throw new InvalidOperationException("Only PNG and JPEG files are supported.")
        };

        // Check file size
        if (e.File.Size > maxFileSize)
        {
            throw new InvalidOperationException($"File size exceeds {maxFileMBSize}MB limit.");
        }

        // Get image dimensions
        int originalWidth, originalHeight;
        using (var tempStream = new MemoryStream())
        {
            // Copy the file stream to a temporary stream to read metadata
            await e.File.OpenReadStream(maxFileSize).CopyToAsync(tempStream);
            tempStream.Position = 0; // Reset stream position for reading

            // Use ImageSharp to get image dimensions
            using (var image = await Image.LoadAsync(tempStream))
            {
                originalWidth = image.Width;
                originalHeight = image.Height;
            }
        }

        // Validate dimensions
        if (originalWidth > maxOriginalWidth || originalHeight > maxOriginalHeight)
        {
            throw new InvalidOperationException($"Image dimensions ({originalWidth}x{originalHeight}) exceed maximum allowed ({maxOriginalWidth}x{maxOriginalHeight}).");
        }

        // Process original image
        var originalImageStream = await e.File.RequestImageFileAsync(format, originalWidth, originalHeight);
        using var memoryStream = new MemoryStream();
        await originalImageStream.OpenReadStream(maxFileSize).CopyToAsync(memoryStream);
        byte[] bufferOriginalFile = memoryStream.ToArray();
        newImage.ImgString = $"data:{format};base64,{Convert.ToBase64String(bufferOriginalFile)}";

        // Process thumbnail
        var resizedImageStream = await e.File.RequestImageFileAsync(format, thumbnailWidth, thumbnailHeight);
        using var thumbnailMemoryStream = new MemoryStream();
        await resizedImageStream.OpenReadStream(maxFileSize).CopyToAsync(thumbnailMemoryStream);
        byte[] bufferThumbnail = thumbnailMemoryStream.ToArray();
        newImage.ImgThumbString = $"data:{format};base64,{Convert.ToBase64String(bufferThumbnail)}";

        // Optionally store dimensions in PlotPlan
        newImage.Width = originalWidth;
        newImage.Height = originalHeight;

        return newImage;
    }
    catch (Exception ex)
    {
        // Log the error (use your preferred logging mechanism)
        Console.WriteLine($"Error processing image: {ex.Message}");
        throw; // Rethrow or handle as needed
    }
}
    
    
    
    
}