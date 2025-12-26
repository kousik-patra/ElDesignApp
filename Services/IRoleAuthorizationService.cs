using System.Text.Json;
using ElDesignApp.Data;
using ElDesignApp.Models;
using Microsoft.AspNetCore.Identity;

namespace ElDesignApp.Services;

public interface IRoleAuthorizationService
{
    Task<List<string>> GetHardRolesForUser(ApplicationUser user);
    Task<List<string>> GetHardRolesForCustomRole(string customRole);
    Task<bool> UserHasAnyHardRole(ApplicationUser user, params string[] requiredHardRoles);
    Task<List<RoleMapping>> GetAllRoleMappings();
    Task<RoleMapping?> GetRoleMappingByName(string customRoleName);
    Task SaveRoleMapping(RoleMapping mapping);
    Task DeleteRoleMapping(string customRoleName);
}


public class RoleAuthorizationService : IRoleAuthorizationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDataRetrievalService _dataService;
    private readonly ILogger<RoleAuthorizationService> _logger;
    private List<RoleMapping>? _cachedMappings;
    private DateTime _cacheExpiry = DateTime.MinValue;
    private readonly TimeSpan _cacheLifetime = TimeSpan.FromMinutes(5);

    public RoleAuthorizationService(
        IServiceProvider serviceProvider,
        IDataRetrievalService dataService,
        ILogger<RoleAuthorizationService> logger)
    {
        _serviceProvider = serviceProvider;
        _dataService = dataService;
        _logger = logger;
    }

    public async Task<List<string>> GetHardRolesForUser(ApplicationUser user)
    {
        using var scope = _serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        var userCustomRoles = await userManager.GetRolesAsync(user);
        var allMappings = await GetAllRoleMappings();
        var hardRoles = new HashSet<string>();
        
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
                            hardRoles.Add(role);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error deserializing roles for {customRole}");
                }
            }
        }
        
        return hardRoles.ToList();
    }

    public async Task<List<string>> GetHardRolesForCustomRole(string customRole)
    {
        var mapping = await GetRoleMappingByName(customRole);
        
        if (mapping == null || !mapping.IsActive)
            return new List<string>();
        
        try
        {
            return JsonSerializer.Deserialize<List<string>>(mapping.MappedHardRoles) ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deserializing roles for {customRole}");
            return new List<string>();
        }
    }

    public async Task<bool> UserHasAnyHardRole(ApplicationUser user, params string[] requiredHardRoles)
    {
        try
        {
            var userHardRoles = await GetHardRolesForUser(user);
            return requiredHardRoles.Any(required => 
                userHardRoles.Contains(required, StringComparer.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking user hard roles");
            return false;
        }
    }

    public async Task<List<RoleMapping>> GetAllRoleMappings()
    {
        // Simple cache to avoid hitting DB on every auth check
        if (_cachedMappings != null && DateTime.Now < _cacheExpiry)
        {
            return _cachedMappings;
        }

        try
        {
            // Use DataRetrievalService which uses Dapper
            var result = await _dataService.ReadFromCacheOrDb<RoleMapping>();
            _cachedMappings = result.Item1;
            _cacheExpiry = DateTime.Now.Add(_cacheLifetime);
            
            return _cachedMappings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting role mappings");
            return new List<RoleMapping>();
        }
    }

    public async Task<RoleMapping?> GetRoleMappingByName(string customRoleName)
    {
        var mappings = await GetAllRoleMappings();
        return mappings.FirstOrDefault(m => 
            m.CustomRoleName.Equals(customRoleName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task SaveRoleMapping(RoleMapping mapping)
    {
        // Will implement later with MyTableService
        throw new NotImplementedException("Use RoleMappingModal for now");
    }

    public async Task DeleteRoleMapping(string customRoleName)
    {
        // Will implement later with MyTableService
        throw new NotImplementedException("Use RoleMappingModal for now");
    }
}