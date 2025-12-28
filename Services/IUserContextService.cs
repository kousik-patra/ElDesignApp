using ElDesignApp.Services.Global;
using Microsoft.AspNetCore.Components.Authorization;

namespace ElDesignApp.Services;

// <summary>
/// Provides current user and project context for services
/// </summary>
public interface IUserContextService
{
    /// <summary>
    /// Get the current authenticated user's name
    /// </summary>
    Task<string?> GetUserNameAsync();
    
    /// <summary>
    /// Get the current project ID
    /// </summary>
    Task<string?> GetProjectIdAsync();
    
    /// <summary>
    /// Get both user name and project ID
    /// </summary>
    Task<(string? UserName, string? ProjectId)> GetContextAsync();
}

public class UserContextService : IUserContextService
{
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly IGlobalDataService _globalData;
    
    // Optional: cache values to avoid repeated async calls
    private string? _cachedUserName;
    private bool _userNameCached;

    public UserContextService(
        AuthenticationStateProvider authStateProvider,
        IGlobalDataService globalData)
    {
        _authStateProvider = authStateProvider ?? throw new ArgumentNullException(nameof(authStateProvider));
        _globalData = globalData ?? throw new ArgumentNullException(nameof(globalData));
    }

    public async Task<string?> GetUserNameAsync()
    {
        if (_userNameCached)
            return _cachedUserName;

        try
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            _cachedUserName = authState.User?.Identity?.Name ?? "System";
            _userNameCached = true;
            return _cachedUserName;
        }
        catch
        {
            return "System";
        }
    }

    public Task<string?> GetProjectIdAsync()
    {
        return Task.FromResult(_globalData.SelectedProject?.Tag);
    }

    public async Task<(string? UserName, string? ProjectId)> GetContextAsync()
    {
        var userName = await GetUserNameAsync();
        var projectId = await GetProjectIdAsync();
        return (userName, projectId);
    }
}