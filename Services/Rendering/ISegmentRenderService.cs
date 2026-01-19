// Services/Rendering/ISegmentRenderService.cs

using System.Text.Json;
using ElDesignApp.Models;
using ElDesignApp.Services.Global;
using Microsoft.JSInterop;

namespace ElDesignApp.Services.Rendering;

/// <summary>
/// Service for rendering cable tray segments in the 3D scene.
/// Handles batch rendering, selection, highlighting, and visibility.
/// </summary>
public interface ISegmentRenderService
{
    /// <summary>
    /// Event fired when segments finish rendering
    /// </summary>
    event Action<int>? OnSegmentsRendered;
    
    /// <summary>
    /// Event fired when segment selection changes
    /// </summary>
    event Action<List<string>>? OnSelectionChanged;
    
    /// <summary>
    /// Whether segments are currently drawn in the scene
    /// </summary>
    bool IsRendered { get; }
    
    /// <summary>
    /// Whether segments are currently visible
    /// </summary>
    bool IsVisible { get; }
    
    /// <summary>
    /// Currently selected segment tags (supports multi-selection)
    /// </summary>
    IReadOnlyList<string> SelectedTags { get; }
    
    /// <summary>
    /// Draw all segments from GlobalData.Segments to the scene
    /// </summary>
    Task DrawSegmentsAsync();
    
    /// <summary>
    /// Draw a specific list of segments
    /// </summary>
    Task DrawSegmentsAsync(List<Segment> segments);
    
    /// <summary>
    /// Clear all segments from the scene
    /// </summary>
    Task ClearSegmentsAsync();
    
    /// <summary>
    /// Set visibility of all segments without removing them
    /// </summary>
    Task SetVisibilityAsync(bool visible);
    
    /// <summary>
    /// Select a single segment (clears previous selection)
    /// </summary>
    Task SelectSegmentAsync(string tag);
    
    /// <summary>
    /// Select multiple segments (clears previous selection)
    /// </summary>
    Task SelectSegmentsAsync(IEnumerable<string> tags);
    
    /// <summary>
    /// Add a segment to the current selection
    /// </summary>
    Task AddToSelectionAsync(string tag);
    
    /// <summary>
    /// Remove a segment from the current selection
    /// </summary>
    Task RemoveFromSelectionAsync(string tag);
    
    /// <summary>
    /// Toggle a segment's selection state
    /// </summary>
    Task ToggleSelectionAsync(string tag);
    
    /// <summary>
    /// Clear all selections
    /// </summary>
    Task ClearSelectionAsync();
    
    /// <summary>
    /// Highlight segments temporarily (for hover effects)
    /// </summary>
    Task HighlightSegmentsAsync(IEnumerable<string> tags, bool highlight = true);
    
    /// <summary>
    /// Focus camera on a specific segment
    /// </summary>
    Task FocusOnSegmentAsync(string tag);
    
    /// <summary>
    /// Focus camera to show all selected segments
    /// </summary>
    Task FocusOnSelectionAsync();
    
    /// <summary>
    /// Update a single segment's appearance (e.g., after data change)
    /// </summary>
    Task UpdateSegmentAsync(Segment segment);
    
    /// <summary>
    /// Get segment info by tag (for tooltips/info panels)
    /// </summary>
    Task<SegmentRenderInfo?> GetSegmentInfoAsync(string tag);
}


/// <summary>
/// Service for rendering cable tray segments in the 3D scene.
/// Encapsulates all segment rendering logic to keep SharedSceneHost clean.
/// </summary>
public class SegmentRenderService : ISegmentRenderService, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IGlobalDataService _globalData;
    private readonly ILayoutFunctionService _layoutFunction;
    
    private readonly List<string> _selectedTags = new();
    private readonly JsonSerializerOptions _jsonOptions = new() { IncludeFields = true };
    
    private bool _isRendered;
    private bool _isVisible;
    private bool _isInitialized;

    public event Action<int>? OnSegmentsRendered;
    public event Action<List<string>>? OnSelectionChanged;

    public bool IsRendered => _isRendered;
    public bool IsVisible => _isVisible;
    public IReadOnlyList<string> SelectedTags => _selectedTags.AsReadOnly();

    public SegmentRenderService(
        IJSRuntime jsRuntime, 
        IGlobalDataService globalData,
        ILayoutFunctionService layoutFunction)
    {
        _jsRuntime = jsRuntime;
        _globalData = globalData;
        _layoutFunction = layoutFunction;
        
        Console.WriteLine("SegmentRenderService: Created");
    }

    /// <summary>
    /// Draw all segments from GlobalData.Segments
    /// </summary>
    public async Task DrawSegmentsAsync()
    {
        Console.WriteLine($"[SegmentRenderService] DrawSegmentsAsync called");
        //Console.WriteLine(Environment.StackTrace);
        
        if (_globalData.Segments == null) return;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var segments = _globalData.Segments.Take(1000).ToList();
        //var segments = _globalData.Segments.Where(seg=> seg.Tag != null && 
        //                                                (seg.Tag.Contains("A11A-ECL-L058_B1-8")|| seg.Tag.Contains("A11A-ECL-L058_B1-9"))).ToList();
    
        //Console.WriteLine($"[TEST] Rendering {segments.Count} segments");

        await DrawSegmentsAsync(_globalData.Segments);
        //await DrawSegmentsAsync(segments);
    }

    /// <summary>
    /// Draw a specific list of segments
    /// </summary>
    public async Task DrawSegmentsAsync(List<Segment> segments)
    {
        if (segments == null || segments.Count == 0)
        {
            Console.WriteLine("SegmentRenderService.DrawSegmentsAsync: No segments to draw");
            return;
        }

        Console.WriteLine($"SegmentRenderService.DrawSegmentsAsync: Drawing {segments.Count} segments...");

        
        try
        {
            // Prepare batch data
            var tags = new List<string>();
            var jsonPointsArray = new List<string>();
            var colors = new List<string>();
            var opacities = new List<float>();
            var opacity = 1.0f;

            var colour = "[128, 128, 204]";

            foreach (var seg in segments)
            {
                try
                {
                    // Determine color based on segment type
                    int[] color = GetSegmentColor(seg);
                    
                    // Generate JSON points for the ladder mesh
                    var jsonPoints = _layoutFunction.DrawLadderJSONPoints(
                        seg.Width, seg.Height, seg.End1, seg.End2, seg.Face);

                    tags.Add(seg.Tag);
                    jsonPointsArray.Add(jsonPoints);
                    colors.Add(JsonSerializer.Serialize(color, _jsonOptions));
                    opacities.Add(seg.Isolated ? 0.5f : 0.7f);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error preparing segment {seg.Tag}: {ex.Message}");
                }
            }

            if (tags.Count > 0)
            {
                // Call JavaScript to draw all segments in batch
                await _jsRuntime.InvokeVoidAsync(
                    "drawSegmentsBatch",
                    tags.ToArray(),
                    jsonPointsArray.ToArray(),
                    colour,
                    //colors.ToArray(),
                    //opacities.ToArray()
                    opacity);

                _isRendered = true;
                _isVisible = true;
                
                Console.WriteLine($"SegmentRenderService: Successfully drew {tags.Count} segments");
                OnSegmentsRendered?.Invoke(tags.Count);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SegmentRenderService.DrawSegmentsAsync ERROR: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Clear all segments from the scene
    /// </summary>
    public async Task ClearSegmentsAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("clearSegments");
            _isRendered = false;
            _isVisible = false;
            _selectedTags.Clear();
            Console.WriteLine("SegmentRenderService: Segments cleared");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SegmentRenderService.ClearSegmentsAsync ERROR: {ex.Message}");
        }
    }

    /// <summary>
    /// Set visibility of all segments
    /// </summary>
    public async Task SetVisibilityAsync(bool visible)
    {
        if (!_isRendered) return;

        try
        {
            await _jsRuntime.InvokeVoidAsync("setSegmentVisibility", visible);
            _isVisible = visible;
            Console.WriteLine($"SegmentRenderService: Visibility set to {visible}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SegmentRenderService.SetVisibilityAsync ERROR: {ex.Message}");
        }
    }

    /// <summary>
    /// Select a single segment (clears previous selection)
    /// </summary>
    public async Task SelectSegmentAsync(string tag)
    {
        await SelectSegmentsAsync(new[] { tag });
    }

    /// <summary>
    /// Select multiple segments (clears previous selection)
    /// </summary>
    public async Task SelectSegmentsAsync(IEnumerable<string> tags)
    {
        if (!_isRendered) return;

        try
        {
            var tagList = tags.ToList();
            
            // Clear previous selection visually
            if (_selectedTags.Count > 0)
            {
                await _jsRuntime.InvokeVoidAsync("setSegmentsSelected", 
                    _selectedTags.ToArray(), false);
            }

            // Update selection state
            _selectedTags.Clear();
            _selectedTags.AddRange(tagList);

            // Apply new selection visually
            if (tagList.Count > 0)
            {
                await _jsRuntime.InvokeVoidAsync("setSegmentsSelected", 
                    tagList.ToArray(), true);
            }

            Console.WriteLine($"SegmentRenderService: Selected {tagList.Count} segments: {string.Join(", ", tagList)}");
            OnSelectionChanged?.Invoke(_selectedTags.ToList());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SegmentRenderService.SelectSegmentsAsync ERROR: {ex.Message}");
        }
    }

    /// <summary>
    /// Add a segment to the current selection
    /// </summary>
    public async Task AddToSelectionAsync(string tag)
    {
        if (!_isRendered || _selectedTags.Contains(tag)) return;

        try
        {
            _selectedTags.Add(tag);
            await _jsRuntime.InvokeVoidAsync("setSegmentsSelected", 
                new[] { tag }, true);
            
            Console.WriteLine($"SegmentRenderService: Added {tag} to selection. Total: {_selectedTags.Count}");
            OnSelectionChanged?.Invoke(_selectedTags.ToList());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SegmentRenderService.AddToSelectionAsync ERROR: {ex.Message}");
        }
    }

    /// <summary>
    /// Remove a segment from the current selection
    /// </summary>
    public async Task RemoveFromSelectionAsync(string tag)
    {
        if (!_isRendered || !_selectedTags.Contains(tag)) return;

        try
        {
            _selectedTags.Remove(tag);
            await _jsRuntime.InvokeVoidAsync("setSegmentsSelected", 
                new[] { tag }, false);
            
            Console.WriteLine($"SegmentRenderService: Removed {tag} from selection. Total: {_selectedTags.Count}");
            OnSelectionChanged?.Invoke(_selectedTags.ToList());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SegmentRenderService.RemoveFromSelectionAsync ERROR: {ex.Message}");
        }
    }

    /// <summary>
    /// Toggle a segment's selection state
    /// </summary>
    public async Task ToggleSelectionAsync(string tag)
    {
        if (_selectedTags.Contains(tag))
        {
            await RemoveFromSelectionAsync(tag);
        }
        else
        {
            await AddToSelectionAsync(tag);
        }
    }

    /// <summary>
    /// Clear all selections
    /// </summary>
    public async Task ClearSelectionAsync()
    {
        if (!_isRendered || _selectedTags.Count == 0) return;

        try
        {
            await _jsRuntime.InvokeVoidAsync("setSegmentsSelected", 
                _selectedTags.ToArray(), false);
            _selectedTags.Clear();
            
            Console.WriteLine("SegmentRenderService: Selection cleared");
            OnSelectionChanged?.Invoke(_selectedTags.ToList());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SegmentRenderService.ClearSelectionAsync ERROR: {ex.Message}");
        }
    }

    /// <summary>
    /// Highlight segments temporarily (for hover effects)
    /// </summary>
    public async Task HighlightSegmentsAsync(IEnumerable<string> tags, bool highlight = true)
    {
        if (!_isRendered) return;

        try
        {
            await _jsRuntime.InvokeVoidAsync("highlightSegments", 
                tags.ToArray(), highlight);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SegmentRenderService.HighlightSegmentsAsync ERROR: {ex.Message}");
        }
    }

    /// <summary>
    /// Focus camera on a specific segment
    /// </summary>
    public async Task FocusOnSegmentAsync(string tag)
    {
        if (!_isRendered) return;

        try
        {
            await _jsRuntime.InvokeVoidAsync("focusOnSegment", tag);
            Console.WriteLine($"SegmentRenderService: Focused on {tag}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SegmentRenderService.FocusOnSegmentAsync ERROR: {ex.Message}");
        }
    }

    /// <summary>
    /// Focus camera to show all selected segments
    /// </summary>
    public async Task FocusOnSelectionAsync()
    {
        if (!_isRendered || _selectedTags.Count == 0) return;

        try
        {
            await _jsRuntime.InvokeVoidAsync("focusOnSegments", _selectedTags.ToArray());
            Console.WriteLine($"SegmentRenderService: Focused on {_selectedTags.Count} selected segments");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SegmentRenderService.FocusOnSelectionAsync ERROR: {ex.Message}");
        }
    }

    /// <summary>
    /// Update a single segment's appearance
    /// </summary>
    public async Task UpdateSegmentAsync(Segment segment)
    {
        if (!_isRendered) return;

        try
        {
            int[] color = GetSegmentColor(segment);
            var jsonPoints = _layoutFunction.DrawLadderJSONPoints(
                segment.Width, segment.Height, segment.End1, segment.End2, segment.Face);
            
            await _jsRuntime.InvokeVoidAsync("updateSegment",
                segment.Tag,
                jsonPoints,
                JsonSerializer.Serialize(color, _jsonOptions),
                segment.Isolated ? 0.5f : 0.7f);
                
            Console.WriteLine($"SegmentRenderService: Updated segment {segment.Tag}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SegmentRenderService.UpdateSegmentAsync ERROR: {ex.Message}");
        }
    }

    /// <summary>
    /// Get segment info by tag
    /// </summary>
    public async Task<SegmentRenderInfo?> GetSegmentInfoAsync(string tag)
    {
        if (!_isRendered) return null;

        try
        {
            var result = await _jsRuntime.InvokeAsync<SegmentRenderInfo?>("getSegmentInfo", tag);
            if (result != null)
            {
                result.IsSelected = _selectedTags.Contains(tag);
            }
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SegmentRenderService.GetSegmentInfoAsync ERROR: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Determine segment color based on type
    /// </summary>
    private int[] GetSegmentColor(Segment seg)
    {
        if (seg.Isolated)
        {
            return _globalData.isolatedSegmentColor;
        }
        else if (seg.AllowableTypes?.Contains("LV") == true)
        {
            return _globalData.segmentColorLV;
        }
        else if (seg.AllowableTypes?.Contains("HV") == true)
        {
            return _globalData.segmentColorHV;
        }
        else
        {
            return _globalData.segmentColorNeutral;
        }
    }

    public async ValueTask DisposeAsync()
    {
        // Clean up if needed
        _selectedTags.Clear();
        Console.WriteLine("SegmentRenderService: Disposed");
    }
}


/// <summary>
/// Information about a rendered segment
/// </summary>
public class SegmentRenderInfo
{
    public string Tag { get; set; } = "";
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public bool IsSelected { get; set; }
    public bool IsHighlighted { get; set; }
}