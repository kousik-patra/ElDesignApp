using Microsoft.AspNetCore.Components.Forms;
using System.Diagnostics;

namespace ElDesignApp.Services;

using ElDesignApp.Models;
using ElDesignApp.Services.Cache;
using ElDesignApp.Services.Global;
using SixLabors.ImageSharp;
using ElDesignApp.Services.DataBase;

using PDFtoImage;
using SkiaSharp;

public interface IPlotPlanService
{
    /// <summary>
    /// Load and process an uploaded image file
    /// </summary>
    Task<PlotPlan> LoadImageAsync(InputFileChangeEventArgs e);
    
    /// <summary>
    /// Get all plot plans for the current project
    /// </summary>
    Task<List<PlotPlan>> GetAllAsync();
    
    /// <summary>
    /// Save a new plot plan to the database
    /// </summary>
    Task<bool> SavePlotPlanAsync(PlotPlan plan);
    
    /// <summary>
    /// Update an existing plot plan
    /// </summary>
    Task<bool> UpdatePlotPlanAsync(PlotPlan plan, params string[] fieldNames);
    
    /// <summary>
    /// Delete a plot plan from the database
    /// </summary>
    Task<bool> DeletePlotPlanAsync(PlotPlan plan);
    
    /// <summary>
    /// Promote a plan to be the new key plan
    /// </summary>
    Task<bool> PromoteToKeyPlanAsync(PlotPlan newKeyPlan, PlotPlan? oldKeyPlan = null);
    
    /// <summary>
    /// Get the current key plan for the project
    /// </summary>
    Task<PlotPlan?> GetKeyPlanAsync();
    
    /// <summary>
    /// Refresh plot plans from database
    /// </summary>
    Task RefreshAsync();
    
    Task<PlotPlan> LoadPdfAsync(InputFileChangeEventArgs e, int pageNumber = 0, int dpi = 150);


    public CoordinateDisplayModel GetCoordinatesForDisplay(double sceneX, double sceneY);

}

public class PlotPlanService : IPlotPlanService
{

    private readonly IDataRetrievalService _dataService;
    private readonly ITableService _tableService;
    private readonly IGlobalDataService _globalData;

    // Image processing constants
    private const int MaxFileSizeMB = 80;
    private const long MaxFileSize = MaxFileSizeMB * 1024 * 1024;
    private const int MaxOriginalWidth = 192000;
    private const int MaxOriginalHeight = 108000;
    private const int ThumbnailWidth = 400;
    private const int ThumbnailHeight = 250;

    public PlotPlanService(
        IDataRetrievalService dataService,
        ITableService tableService,
        IGlobalDataService globalData)
    {
        _dataService = dataService;
        _tableService = tableService;
        _globalData = globalData;
    }
    
    
        #region Image Processing

    /// <inheritdoc />
    public async Task<PlotPlan> LoadImageAsync(InputFileChangeEventArgs e)
    {
        try
        {
            var newImage = new PlotPlan
            {
                UID = Guid.NewGuid()
            };

            // Validate file type
            string format = e.File.ContentType.ToLower() switch
            {
                "image/png" => "image/png",
                "image/jpeg" => "image/jpeg",
                _ => throw new InvalidOperationException("Only PNG and JPEG files are supported.")
            };

            // Check file size
            if (e.File.Size > MaxFileSize)
            {
                throw new InvalidOperationException($"File size exceeds {MaxFileSizeMB}MB limit.");
            }

            // Get image dimensions
            int originalWidth, originalHeight;
            using (var tempStream = new MemoryStream())
            {
                await e.File.OpenReadStream(MaxFileSize).CopyToAsync(tempStream);
                tempStream.Position = 0;

                using var image = await Image.LoadAsync(tempStream);
                originalWidth = image.Width;
                originalHeight = image.Height;
            }

            // Validate dimensions
            if (originalWidth > MaxOriginalWidth || originalHeight > MaxOriginalHeight)
            {
                throw new InvalidOperationException(
                    $"Image dimensions ({originalWidth}x{originalHeight}) exceed maximum allowed ({MaxOriginalWidth}x{MaxOriginalHeight}).");
            }

            // Process original image
            var originalImageStream = await e.File.RequestImageFileAsync(format, originalWidth, originalHeight);
            using var memoryStream = new MemoryStream();
            await originalImageStream.OpenReadStream(MaxFileSize).CopyToAsync(memoryStream);
            byte[] bufferOriginalFile = memoryStream.ToArray();
            newImage.ImgString = $"data:{format};base64,{Convert.ToBase64String(bufferOriginalFile)}";

            // Process thumbnail
            var resizedImageStream = await e.File.RequestImageFileAsync(format, ThumbnailWidth, ThumbnailHeight);
            using var thumbnailMemoryStream = new MemoryStream();
            await resizedImageStream.OpenReadStream(MaxFileSize).CopyToAsync(thumbnailMemoryStream);
            byte[] bufferThumbnail = thumbnailMemoryStream.ToArray();
            newImage.ImgThumbString = $"data:{format};base64,{Convert.ToBase64String(bufferThumbnail)}";

            // Store dimensions
            newImage.Width = originalWidth;
            newImage.Height = originalHeight;

            Debug.WriteLine($"Image loaded: {originalWidth}x{originalHeight}, Size: {e.File.Size / 1024}KB");
            return newImage;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error processing image: {ex.Message}");
            throw;
        }
    }

    #endregion

    #region CRUD Operations

    /// <inheritdoc />
    public async Task<List<PlotPlan>> GetAllAsync()
    {
        try
        {
            // Refresh cache and get plot plans from database
            var (plotPlans, _, _, _) = await _dataService.RefreshCacheAndReadFromDb<PlotPlan>();
            
            // Update global data
            _globalData.PlotPlans = plotPlans ?? new List<PlotPlan>();
            
            // Filter by current project if one is selected
            if (_globalData.SelectedProject != null)
            {
                return _globalData.PlotPlans
                    .Where(p => p.ProjectId == _globalData.SelectedProject.Tag)
                    .OrderByDescending(p => p.KeyPlan)  // Key plan first
                    .ThenBy(p => p.UpdatedOn)
                    .ToList();
            }

            return _globalData.PlotPlans
                .OrderByDescending(p => p.KeyPlan)
                .ThenBy(p => p.UpdatedOn)
                .ToList();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting plot plans: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SavePlotPlanAsync(PlotPlan plan)
    {
        try
        {
            // Set audit fields
            plan.UpdatedOn = DateTime.Now;
            
            // If this is the first plan, make it the key plan
            var existingPlans = await GetAllAsync();
            if (!existingPlans.Any())
            {
                plan.KeyPlan = true;
                
                // Update project's global reference
                if (_globalData.SelectedProject != null)
                {
                    _globalData.SelectedProject.XEW = plan.XEW;
                    _globalData.SelectedProject.GlobalE = plan.GlobalE;
                    _globalData.SelectedProject.GlobalN = plan.GlobalN;

                    _globalData.SelectedProject.LocalE = plan.LocalE;
                    _globalData.SelectedProject.LocalN = plan.LocalN;
                    
                    _globalData.SelectedProject.PositiveScaleX = plan.X2>plan.X1;
                    _globalData.SelectedProject.PositiveScaleY = plan.Y2>plan.Y1;
                    
                    _globalData.SelectedProject.AngleTrueNorth = plan.AngleTrueNorth;
                    
                    await _tableService.UpdateAsync(_globalData.SelectedProject);
                }
            }

            await _tableService.InsertAsync(plan);
            
            // Refresh cache
            await RefreshAsync();
            
            Debug.WriteLine($"Plot plan '{plan.Tag}' saved successfully. KeyPlan: {plan.KeyPlan}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving plot plan: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> UpdatePlotPlanAsync(PlotPlan plan, params string[] fieldNames)
    {
        try
        {
            plan.UpdatedOn = DateTime.Now;
            
            // Include UpdatedOn in the fields to update
            var fieldsToUpdate = fieldNames.Concat(new[] { "UpdatedOn" }).Distinct().ToArray();
            
            await _tableService.UpdateFieldsAsync(plan, fieldsToUpdate);
            
            Debug.WriteLine($"Plot plan '{plan.Tag}' updated. Fields: {string.Join(", ", fieldNames)}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error updating plot plan: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeletePlotPlanAsync(PlotPlan plan)
    {
        try
        {
            await _tableService.DeleteAsync(plan);
            
            // Remove from global data
            _globalData.PlotPlans?.Remove(plan);
            
            // Refresh cache
            await RefreshAsync();
            
            Debug.WriteLine($"Plot plan '{plan.Tag}' deleted successfully.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error deleting plot plan: {ex.Message}");
            throw;
        }
    }

    #endregion

    #region Key Plan Management

    /// <inheritdoc />
    public async Task<PlotPlan?> GetKeyPlanAsync()
    {
        var plans = await GetAllAsync();
        return plans.FirstOrDefault(p => p.KeyPlan);
    }

    /// <inheritdoc />
    public async Task<bool> PromoteToKeyPlanAsync(PlotPlan newKeyPlan, PlotPlan? oldKeyPlan = null)
    {
        try
        {
            Debug.WriteLine($"Promoting '{newKeyPlan.Tag}' to key plan");

            // Get old key plan if not provided
            oldKeyPlan ??= await GetKeyPlanAsync();

            if (oldKeyPlan != null && oldKeyPlan.UID != newKeyPlan.UID)
            {
                // Calculate delta for repositioning
                float deltaE = oldKeyPlan.GlobalE - newKeyPlan.GlobalE;
                float deltaN = oldKeyPlan.GlobalN - newKeyPlan.GlobalN;

                // Remove key plan status from old plan
                oldKeyPlan.KeyPlan = false;
                await _tableService.UpdateFieldsAsync(oldKeyPlan, "KeyPlan");

                // Reposition all other plans relative to new key plan
                var allPlans = await GetAllAsync();
                foreach (var plan in allPlans.Where(p => p.UID != newKeyPlan.UID && p.UID != oldKeyPlan.UID))
                {
                    plan.CentreX += deltaE;
                    plan.CentreY += deltaN;
                    await _tableService.UpdateFieldsAsync(plan, "CentreX", "CentreY");
                }
            }

            // Update project reference point
            if (_globalData.SelectedProject != null)
            {
                _globalData.SelectedProject.GlobalE = newKeyPlan.GlobalE;
                _globalData.SelectedProject.GlobalN = newKeyPlan.GlobalN;
                await _tableService.UpdateFieldsAsync(_globalData.SelectedProject, "GlobalE", "GlobalN");
            }

            // Mark new plan as key plan
            newKeyPlan.KeyPlan = true;
            await _tableService.UpdateFieldsAsync(newKeyPlan, "KeyPlan");

            // Refresh cache
            await RefreshAsync();

            Debug.WriteLine($"'{newKeyPlan.Tag}' is now the key plan.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error promoting to key plan: {ex.Message}");
            throw;
        }
    }

    #endregion

    #region Utility Methods

    /// <inheritdoc />
    public async Task RefreshAsync()
    {
        try
        {
            var (plotPlans, _, _, _) = await _dataService.RefreshCacheAndReadFromDb<PlotPlan>();
            _globalData.PlotPlans = plotPlans ?? new List<PlotPlan>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error refreshing plot plans: {ex.Message}");
            throw;
        }
    }

    #endregion

    /// <summary>
    /// Load a PDF file and convert to image for Three.js
    /// </summary>
    public async Task<PlotPlan> LoadPdfAsync(InputFileChangeEventArgs e, int pageNumber = 0, int dpi = 150)
    {
        try
        {
            var newImage = new PlotPlan
            {
                UID = Guid.NewGuid()
            };
    
            // Validate file type
            if (e.File.ContentType.ToLower() != "application/pdf")
            {
                throw new InvalidOperationException("Only PDF files are supported by this method.");
            }
    
            // Check file size (50MB max for PDFs)
            const long maxPdfSize = 50 * 1024 * 1024;
            if (e.File.Size > maxPdfSize)
            {
                throw new InvalidOperationException("PDF file size exceeds 50MB limit.");
            }
    
            // Read PDF into memory
            using var pdfStream = new MemoryStream();
            await e.File.OpenReadStream(maxPdfSize).CopyToAsync(pdfStream);
            pdfStream.Position = 0;
            var pdfBytes = pdfStream.ToArray();
    
            // Get page count
            int pageCount = Conversion.GetPageCount(pdfBytes);
            if (pageNumber >= pageCount)
            {
                throw new InvalidOperationException($"PDF has {pageCount} pages. Page {pageNumber + 1} does not exist.");
            }
    
            // FIXED: Use RenderOptions for DPI setting
            var renderOptions = new RenderOptions
            {
                Dpi = dpi,
                WithAnnotations = true,
                WithFormFill = true
            };
    
            // Render PDF page to SKBitmap
            using var bitmap = Conversion.ToImage(pdfBytes, (Index)pageNumber, null, renderOptions);
    
            // Convert SKBitmap to PNG bytes
            using var pngData = bitmap.Encode(SKEncodedImageFormat.Png, 90);
            var pngBytes = pngData.ToArray();
    
            // Create base64 image string
            newImage.ImgString = $"data:image/png;base64,{Convert.ToBase64String(pngBytes)}";
    
            // Create thumbnail
            var thumbInfo = new SKImageInfo(400, 250);
            using var thumbBitmap = new SKBitmap(thumbInfo);
            bitmap.ScalePixels(thumbBitmap, SKFilterQuality.Medium);
            
            using var thumbData = thumbBitmap.Encode(SKEncodedImageFormat.Png, 80);
            newImage.ImgThumbString = $"data:image/png;base64,{Convert.ToBase64String(thumbData.ToArray())}";
    
            // Store dimensions
            newImage.Width = bitmap.Width;
            newImage.Height = bitmap.Height;
    
            Debug.WriteLine($"PDF loaded: Page {pageNumber + 1}/{pageCount}, {bitmap.Width}x{bitmap.Height}px at {dpi}dpi");
            return newImage;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error processing PDF: {ex.Message}");
            throw;
        }
    }
    
    
    public CoordinateSystemManager CoordinateManager { get; } = new();
    public CoordinateDisplayModel GetCoordinatesForDisplay(double sceneX, double sceneY)
    {
        var allCoords = CoordinateManager.GetAllCoordinates(sceneX, sceneY);
        
        return new CoordinateDisplayModel
        {
            SceneX = sceneX,
            SceneY = sceneY,
            SystemCoordinates = allCoords.Select(kv => new SystemCoordinate
            {
                SystemName = kv.Key,
                X = kv.Value.X,
                Y = kv.Value.Y,
                Unit = CoordinateManager.Get(kv.Key)?.Unit ?? "m"
            }).ToList()
        };
    }
    
    
}