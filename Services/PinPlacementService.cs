// Services/PinPlacementService.cs
// Service to manage pin placement state across components

namespace ElDesignApp.Services;

public class PinPlacementService
{
    // ===== State =====
    private bool _isActive;
    private List<string> _tagList = new();
    private int _currentTagIndex;
    private bool _shiftKeyPressed;

    // ===== Events =====
    public event Action? OnStateChanged;
    public event Action<string, double, double, double>? OnPinPlaced;
    public event Action? OnAllPinsPlaced;

    // ===== Properties =====
    
    /// <summary>
    /// Whether pin placement mode is active
    /// </summary>
    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive != value)
            {
                _isActive = value;
                Console.WriteLine($"PinPlacementService: IsActive = {value}");
                OnStateChanged?.Invoke();
            }
        }
    }

    /// <summary>
    /// Whether Shift key is currently pressed
    /// </summary>
    public bool ShiftKeyPressed
    {
        get => _shiftKeyPressed;
        set
        {
            if (_shiftKeyPressed != value)
            {
                _shiftKeyPressed = value;
                OnStateChanged?.Invoke();
            }
        }
    }

    /// <summary>
    /// Whether cursor should show pin (IsActive AND ShiftKeyPressed)
    /// </summary>
    public bool ShowPinCursor => IsActive && ShiftKeyPressed;

    /// <summary>
    /// Current tag index in the list
    /// </summary>
    public int CurrentTagIndex => _currentTagIndex;

    /// <summary>
    /// Total number of tags
    /// </summary>
    public int TotalTags => _tagList.Count;

    /// <summary>
    /// Number of remaining tags to place
    /// </summary>
    public int RemainingTags => Math.Max(0, _tagList.Count - _currentTagIndex);

    /// <summary>
    /// Whether all tags have been placed
    /// </summary>
    public bool AllTagsPlaced => _currentTagIndex >= _tagList.Count;

    /// <summary>
    /// Get current tag (next to be placed)
    /// </summary>
    public string? CurrentTag => 
        _currentTagIndex < _tagList.Count ? _tagList[_currentTagIndex] : null;

    /// <summary>
    /// Get the tag list
    /// </summary>
    public IReadOnlyList<string> TagList => _tagList.AsReadOnly();

    // ===== Methods =====

    /// <summary>
    /// Start pin placement mode with a list of tags
    /// </summary>
    /// <param name="tags">List of tags to place</param>
    public void StartPlacement(IEnumerable<string> tags)
    {
        _tagList = tags.ToList();
        _currentTagIndex = 0;
        _isActive = true;
        
        Console.WriteLine($"PinPlacementService: Started with {_tagList.Count} tags");
        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// Add more tags to the list
    /// </summary>
    public void AddTags(IEnumerable<string> tags)
    {
        _tagList.AddRange(tags);
        Console.WriteLine($"PinPlacementService: Added tags, total = {_tagList.Count}");
        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// Get the next tag and advance the index
    /// </summary>
    /// <returns>The next tag, or null if all tags are placed</returns>
    public string? GetNextTag()
    {
        if (_currentTagIndex >= _tagList.Count)
        {
            return null;
        }

        var tag = _tagList[_currentTagIndex];
        _currentTagIndex++;

        Console.WriteLine($"PinPlacementService: Tag '{tag}' used, {RemainingTags} remaining");

        if (AllTagsPlaced)
        {
            Console.WriteLine("PinPlacementService: All tags placed!");
            OnAllPinsPlaced?.Invoke();
        }

        OnStateChanged?.Invoke();
        return tag;
    }

    /// <summary>
    /// Record that a pin was placed (called after successful placement)
    /// </summary>
    public void NotifyPinPlaced(string tag, double x, double y, double z)
    {
        OnPinPlaced?.Invoke(tag, x, y, z);
    }

    /// <summary>
    /// Reset to start of tag list
    /// </summary>
    public void Reset()
    {
        _currentTagIndex = 0;
        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// Stop pin placement mode
    /// </summary>
    public void StopPlacement()
    {
        _isActive = false;
        _shiftKeyPressed = false;
        Console.WriteLine("PinPlacementService: Stopped");
        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// Clear everything
    /// </summary>
    public void Clear()
    {
        _tagList.Clear();
        _currentTagIndex = 0;
        _isActive = false;
        _shiftKeyPressed = false;
        OnStateChanged?.Invoke();
    }
}
