using Microsoft.JSInterop;
using System.Diagnostics;

namespace ElDesignApp.Services.Table;

/// <summary>
/// JS Interop service interface for Table component operations
/// </summary>
public interface ITableJsInterop : IAsyncDisposable
{
    /// <summary>
    /// Initializes dynamic resizing for a table container.
    /// Sets up window resize and click event listeners.
    /// </summary>
    /// <param name="elementId">The ID of the container element</param>
    /// <param name="config">Optional configuration for resize behavior</param>
    /// <returns>True if initialization was successful</returns>
    Task<bool> InitializeDynamicResizeAsync(string elementId, TableResizeConfig? config = null);

    /// <summary>
    /// Disposes event listeners for a specific table instance.
    /// Should be called when the component is disposed.
    /// </summary>
    /// <param name="elementId">The ID of the container element</param>
    /// <returns>True if disposal was successful</returns>
    Task<bool> DisposeInstanceAsync(string elementId);

    /// <summary>
    /// Manually triggers a resize calculation for a specific element.
    /// Useful after programmatic changes that affect layout.
    /// </summary>
    /// <param name="elementId">The ID of the container element</param>
    /// <returns>True if resize was triggered successfully</returns>
    Task<bool> TriggerResizeAsync(string elementId);

    /// <summary>
    /// Updates the resize configuration for an existing instance.
    /// </summary>
    /// <param name="elementId">The ID of the container element</param>
    /// <param name="config">New configuration values</param>
    /// <returns>True if update was successful</returns>
    Task<bool> UpdateConfigAsync(string elementId, TableResizeConfig config);

    /// <summary>
    /// Scrolls a specific cell into view within the table.
    /// Uses smooth scrolling behavior.
    /// </summary>
    /// <param name="tableId">The ID of the table element</param>
    /// <param name="rowIndex">Zero-based row index</param>
    /// <param name="colIndex">Zero-based column index</param>
    /// <returns>True if scroll was successful</returns>
    Task<bool> ScrollCellIntoViewAsync(string tableId, int rowIndex, int colIndex);

    /// <summary>
    /// Focuses the input element within a cell for editing.
    /// Also selects all text for easy replacement.
    /// </summary>
    /// <param name="cellSelector">CSS selector for the cell containing the input</param>
    /// <returns>True if focus was successful</returns>
    Task<bool> FocusCellInputAsync(string cellSelector);

    /// <summary>
    /// Downloads data as a file to the user's device.
    /// Used for Excel export functionality.
    /// </summary>
    /// <param name="filename">The name of the file to download</param>
    /// <param name="base64Data">Base64 encoded file content</param>
    /// <param name="mimeType">Optional MIME type (defaults to Excel format)</param>
    Task SaveAsFileAsync(string filename, string base64Data, string? mimeType = null);

    /// <summary>
    /// Gets the current scroll position of a scrollable element.
    /// </summary>
    /// <param name="elementId">The ID of the scrollable container</param>
    /// <returns>ScrollPosition object or null if element not found</returns>
    Task<ScrollPosition?> GetScrollPositionAsync(string elementId);

    /// <summary>
    /// Sets the scroll position of a scrollable element.
    /// Useful for restoring scroll position after re-render.
    /// </summary>
    /// <param name="elementId">The ID of the scrollable container</param>
    /// <param name="scrollTop">Vertical scroll position in pixels</param>
    /// <param name="scrollLeft">Horizontal scroll position in pixels</param>
    /// <returns>True if scroll position was set successfully</returns>
    Task<bool> SetScrollPositionAsync(string elementId, double scrollTop, double scrollLeft);
}

/// <summary>
/// Implementation of ITableJsInterop for Table component JavaScript operations
/// </summary>
public class TableJsInterop : ITableJsInterop
{
    private readonly IJSRuntime _jsRuntime;
    private readonly List<string> _initializedElements = new();
    private bool _disposed;

    /// <summary>
    /// JavaScript module path prefix.
    /// This matches window.ElDesignApp.Table in your bundled JS.
    /// </summary>
    private const string JsPrefix = "ElDesignApp.Table";

    /// <summary>
    /// Default MIME type for Excel files
    /// </summary>
    private const string ExcelMimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public TableJsInterop(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
    }

    /// <inheritdoc />
    public async Task<bool> InitializeDynamicResizeAsync(string elementId, TableResizeConfig? config = null)
    {
        if (string.IsNullOrWhiteSpace(elementId))
        {
            Debug.WriteLine("[TableJsInterop] InitializeDynamicResizeAsync: elementId is null or empty");
            return false;
        }

        // Prevent duplicate initialization
        if (_initializedElements.Contains(elementId))
        {
            Debug.WriteLine($"[TableJsInterop] Element '{elementId}' already initialized");
            return true;
        }

        try
        {
            // Build JS config object (null values are handled in JS)
            object? jsConfig = config != null
                ? new
                {
                    widthPercentage = config.WidthPercentage,
                    minWidth = config.MinWidth,
                    maxWidth = config.MaxWidth
                }
                : null;

            var result = await _jsRuntime.InvokeAsync<bool>(
                $"{JsPrefix}.initializeDynamicResize",
                elementId,
                jsConfig);

            if (result)
            {
                _initializedElements.Add(elementId);
                Debug.WriteLine($"[TableJsInterop] Successfully initialized '{elementId}'");
            }
            else
            {
                Debug.WriteLine($"[TableJsInterop] JS returned false for '{elementId}' - element may not exist");
            }

            return result;
        }
        catch (JSException ex)
        {
            Debug.WriteLine($"[TableJsInterop] JSException initializing '{elementId}': {ex.Message}");
            return false;
        }
        catch (InvalidOperationException ex)
        {
            // This can happen if JS interop is called during prerendering
            Debug.WriteLine($"[TableJsInterop] InvalidOperationException (prerendering?): {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TableJsInterop] Unexpected error initializing '{elementId}': {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DisposeInstanceAsync(string elementId)
    {
        if (string.IsNullOrWhiteSpace(elementId))
        {
            return false;
        }

        // Skip if not initialized
        if (!_initializedElements.Contains(elementId))
        {
            Debug.WriteLine($"[TableJsInterop] Element '{elementId}' was not initialized, skipping dispose");
            return true;
        }

        try
        {
            var result = await _jsRuntime.InvokeAsync<bool>(
                $"{JsPrefix}.dispose",
                elementId);

            // Remove from tracking regardless of JS result
            _initializedElements.Remove(elementId);
            
            Debug.WriteLine($"[TableJsInterop] Disposed '{elementId}': {result}");
            return result;
        }
        catch (JSDisconnectedException)
        {
            // Circuit disconnected, cleanup tracking only
            _initializedElements.Remove(elementId);
            Debug.WriteLine($"[TableJsInterop] Circuit disconnected, cleaned up tracking for '{elementId}'");
            return true;
        }
        catch (JSException ex)
        {
            Debug.WriteLine($"[TableJsInterop] Error disposing '{elementId}': {ex.Message}");
            _initializedElements.Remove(elementId);
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TableJsInterop] Unexpected error disposing '{elementId}': {ex.Message}");
            _initializedElements.Remove(elementId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> TriggerResizeAsync(string elementId)
    {
        if (string.IsNullOrWhiteSpace(elementId))
        {
            return false;
        }

        try
        {
            return await _jsRuntime.InvokeAsync<bool>(
                $"{JsPrefix}.triggerResize",
                elementId);
        }
        catch (JSException ex)
        {
            Debug.WriteLine($"[TableJsInterop] Error triggering resize for '{elementId}': {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TableJsInterop] Unexpected error triggering resize: {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> UpdateConfigAsync(string elementId, TableResizeConfig config)
    {
        if (string.IsNullOrWhiteSpace(elementId))
        {
            throw new ArgumentException("Element ID cannot be null or empty", nameof(elementId));
        }

        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        try
        {
            var jsConfig = new
            {
                widthPercentage = config.WidthPercentage,
                minWidth = config.MinWidth,
                maxWidth = config.MaxWidth
            };

            return await _jsRuntime.InvokeAsync<bool>(
                $"{JsPrefix}.updateConfig",
                elementId,
                jsConfig);
        }
        catch (JSException ex)
        {
            Debug.WriteLine($"[TableJsInterop] Error updating config for '{elementId}': {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TableJsInterop] Unexpected error updating config: {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ScrollCellIntoViewAsync(string tableId, int rowIndex, int colIndex)
    {
        if (string.IsNullOrWhiteSpace(tableId))
        {
            return false;
        }

        if (rowIndex < 0 || colIndex < 0)
        {
            Debug.WriteLine($"[TableJsInterop] Invalid cell indices: row={rowIndex}, col={colIndex}");
            return false;
        }

        try
        {
            return await _jsRuntime.InvokeAsync<bool>(
                $"{JsPrefix}.scrollCellIntoView",
                tableId,
                rowIndex,
                colIndex);
        }
        catch (JSException ex)
        {
            Debug.WriteLine($"[TableJsInterop] Error scrolling to cell [{rowIndex},{colIndex}]: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TableJsInterop] Unexpected error scrolling to cell: {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> FocusCellInputAsync(string cellSelector)
    {
        if (string.IsNullOrWhiteSpace(cellSelector))
        {
            return false;
        }

        try
        {
            return await _jsRuntime.InvokeAsync<bool>(
                $"{JsPrefix}.focusCellInput",
                cellSelector);
        }
        catch (JSException ex)
        {
            Debug.WriteLine($"[TableJsInterop] Error focusing cell input '{cellSelector}': {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TableJsInterop] Unexpected error focusing cell: {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task SaveAsFileAsync(string filename, string base64Data, string? mimeType = null)
    {
        if (string.IsNullOrWhiteSpace(filename))
        {
            throw new ArgumentException("Filename cannot be null or empty", nameof(filename));
        }

        if (string.IsNullOrWhiteSpace(base64Data))
        {
            throw new ArgumentException("Base64 data cannot be null or empty", nameof(base64Data));
        }

        try
        {
            // Use the saveAsFile function (exposed at window level for backward compatibility)
            await _jsRuntime.InvokeVoidAsync(
                "saveAsFile",
                filename,
                base64Data,
                mimeType ?? ExcelMimeType);

            Debug.WriteLine($"[TableJsInterop] File download initiated: {filename}");
        }
        catch (JSException ex)
        {
            Debug.WriteLine($"[TableJsInterop] Error saving file '{filename}': {ex.Message}");
            throw new InvalidOperationException($"Failed to download file '{filename}'", ex);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TableJsInterop] Unexpected error saving file: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ScrollPosition?> GetScrollPositionAsync(string elementId)
    {
        if (string.IsNullOrWhiteSpace(elementId))
        {
            return null;
        }

        try
        {
            return await _jsRuntime.InvokeAsync<ScrollPosition?>(
                $"{JsPrefix}.getScrollPosition",
                elementId);
        }
        catch (JSException ex)
        {
            Debug.WriteLine($"[TableJsInterop] Error getting scroll position for '{elementId}': {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TableJsInterop] Unexpected error getting scroll position: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SetScrollPositionAsync(string elementId, double scrollTop, double scrollLeft)
    {
        if (string.IsNullOrWhiteSpace(elementId))
        {
            return false;
        }

        try
        {
            return await _jsRuntime.InvokeAsync<bool>(
                $"{JsPrefix}.setScrollPosition",
                elementId,
                scrollTop,
                scrollLeft);
        }
        catch (JSException ex)
        {
            Debug.WriteLine($"[TableJsInterop] Error setting scroll position for '{elementId}': {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TableJsInterop] Unexpected error setting scroll position: {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        Debug.WriteLine($"[TableJsInterop] Disposing {_initializedElements.Count} instances...");

        // Create a copy to avoid modification during iteration
        var elementsToDispose = _initializedElements.ToList();

        foreach (var elementId in elementsToDispose)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync($"{JsPrefix}.dispose", elementId);
            }
            catch (JSDisconnectedException)
            {
                // Circuit disconnected, ignore
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TableJsInterop] Error disposing '{elementId}' during DisposeAsync: {ex.Message}");
            }
        }

        _initializedElements.Clear();
        _disposed = true;

        Debug.WriteLine("[TableJsInterop] Disposed");
    }
}

/// <summary>
/// Configuration for table dynamic resizing
/// </summary>
public class TableResizeConfig
{
    /// <summary>
    /// Width as percentage of window (0.0 - 1.0). Default: 0.8 (80%)
    /// </summary>
    public double WidthPercentage { get; set; } = 0.8;

    /// <summary>
    /// Minimum width in pixels. Default: 300
    /// </summary>
    public int MinWidth { get; set; } = 300;

    /// <summary>
    /// Maximum width in pixels. 0 = no maximum. Default: 0
    /// </summary>
    public int MaxWidth { get; set; } = 0;
}

/// <summary>
/// Scroll position data returned from JavaScript
/// </summary>
public class ScrollPosition
{
    /// <summary>
    /// Vertical scroll position in pixels
    /// </summary>
    public double ScrollTop { get; set; }

    /// <summary>
    /// Horizontal scroll position in pixels
    /// </summary>
    public double ScrollLeft { get; set; }
}