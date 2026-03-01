using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

namespace ElDesignApp.Services;

public interface IMiscService
{
    Task<(string? UserId, string? UserName)> GetCurrentUserInfoAsync();
    public event Action<string>? OnTextUpdated;
    void NotifyTextUpdate(string newText);
    public string BuildCopyTag(string sourceTag, string category, Dictionary<(string, string), Guid> allTags);
}


public class MiscService : IMiscService
{

    private readonly AuthenticationStateProvider _authenticationStateProvider;
    
    public MiscService(AuthenticationStateProvider authenticationStateProvider)
    {
        _authenticationStateProvider = authenticationStateProvider;
    }
    
    public async Task<(string? UserId, string? UserName)> GetCurrentUserInfoAsync()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? user.FindFirst("sub")?.Value
                         ?? "unknown";

            var userName = user.Identity.Name ?? user.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";

            return (userId, userName);
        }

        return (null, null);
    }
    
    public event Action<string>? OnTextUpdated;
    public void NotifyTextUpdate(string newText) {
        OnTextUpdated?.Invoke(newText);
    }
    
    public string BuildCopyTag(string sourceTag, string category, Dictionary<(string, string), Guid> allTags)
    {
        // Strip any existing copy suffix — "T1-Copy1" → "T1", "T1-Copy" → "T1"
        var baseTag = System.Text.RegularExpressions.Regex.Replace(sourceTag, @"-Copy\d*$", "");

        // Try "-Copy" first (1st copy)
        var firstCandidate = $"{baseTag}-Copy";
        if (!TagExistsInCategory(category, firstCandidate, allTags))
            return firstCandidate;

        // Subsequent copies: "-Copy1", "-Copy2", ...
        int n = 1;
        while (true)
        {
            var candidate = $"{baseTag}-Copy{n}";
            if (!TagExistsInCategory(category, candidate, allTags))
                return candidate;
            n++;
            if (n > 9999)
                return $"{baseTag}-Copy{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        }
    }

    private bool TagExistsInCategory(string category, string tag, Dictionary<(string, string), Guid> allTags)
    {
        return allTags.Keys.Any(k =>
            string.Equals(k.Item1, category, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(k.Item2, tag,      StringComparison.OrdinalIgnoreCase));
    }
    
    
}
