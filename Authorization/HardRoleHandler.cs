using System.Security.Claims;
using System.Text.Json;
using ElDesignApp.Data;
using ElDesignApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace ElDesignApp.Authorization;

// Authorization/HardRoleHandler.cs - SIMPLER VERSION
public class HardRoleHandler : AuthorizationHandler<HardRoleRequirement>
{
    private readonly IRoleAuthorizationService _roleAuthService;
    private readonly ILogger<HardRoleHandler> _logger;

    public HardRoleHandler(
        IRoleAuthorizationService roleAuthService,
        ILogger<HardRoleHandler> logger)
    {
        _roleAuthService = roleAuthService;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, 
        HardRoleRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            context.Fail();
            return;
        }

        try
        {
            // Get custom roles from user claims (NO DATABASE HIT)
            var userCustomRoles = context.User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            if (!userCustomRoles.Any())
            {
                context.Fail();
                return;
            }

            // Get all role mappings (cached, uses Dapper)
            var allMappings = await _roleAuthService.GetAllRoleMappings();
            
            // Find mapped hard roles
            var userHardRoles = new HashSet<string>();
            
            foreach (var customRole in userCustomRoles)
            {
                var mapping = allMappings.FirstOrDefault(m => 
                    m.CustomRoleName.Equals(customRole, StringComparison.OrdinalIgnoreCase) && 
                    m.IsActive);
                
                if (mapping != null)
                {
                    try
                    {
                        var mappedRoles = JsonSerializer.Deserialize<List<string>>(mapping.MappedHardRoles);
                        if (mappedRoles != null)
                        {
                            foreach (var role in mappedRoles)
                            {
                                userHardRoles.Add(role);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error deserializing roles for {customRole}");
                    }
                }
            }

            // Check if user has any required hard role
            var hasAccess = requirement.HardRoles.Any(required => 
                userHardRoles.Contains(required, StringComparer.OrdinalIgnoreCase));
            
            if (hasAccess)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in HardRoleHandler");
            context.Fail();
        }
    }
}