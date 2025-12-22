using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

namespace ElDesignApp.Services;

public interface IMiscService
{
    Task<(string? UserId, string? UserName)> GetCurrentUserInfoAsync();
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
}
