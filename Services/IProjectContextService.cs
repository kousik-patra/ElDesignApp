using System.Security.Claims;
using ElDesignApp.Data;
using ElDesignApp.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.JSInterop;



namespace ElDesignApp.Services;

/// <summary>
/// Manages the current project context for a logged-in user
/// Stores selected project in session storage
/// </summary>
public interface IProjectContextService
{
    /// <summary>
    /// Gets or sets the current project ID for the user's session
    /// </summary>
    Guid? CurrentProjectId { get; }
    
    /// <summary>
    /// Gets the current project details
    /// </summary>
    Task<Project?> GetCurrentProjectAsync();
    
    /// <summary>
    /// Sets the current project for the user's session
    /// </summary>
    Task SetCurrentProjectAsync(Guid projectId);
    
    /// <summary>
    /// Clears the current project (logout or project change)
    /// </summary>
    Task ClearCurrentProjectAsync();
    
    /// <summary>
    /// Gets all projects the user has access to
    /// </summary>
    Task<List<Project>> GetUserProjectsAsync(string userId);
    
    /// <summary>
    /// Checks if user is an Admin in the current project
    /// </summary>
    Task<bool> IsUserAdminInCurrentProjectAsync(string userId);
    
    /// <summary>
    /// Checks if user is an Admin in a specific project
    /// </summary>
    Task<bool> IsUserAdminInProjectAsync(string userId, Guid projectId);
    
    /// <summary>
    /// Gets user's custom roles in the current project
    /// </summary>
    Task<List<string>> GetUserCustomRolesInCurrentProjectAsync(string userId);
    
    /// <summary>
    /// Gets user's hard roles in the current project (via custom role mappings)
    /// </summary>
    Task<List<string>> GetUserHardRolesInCurrentProjectAsync(string userId);
    
    /// <summary>
    /// Gets user's hard roles in a specific project (via custom role mappings)
    /// </summary>
    Task<List<string>> GetUserHardRolesInProjectAsync(string userId, Guid projectId);
    
}

public class ProjectContextService : IProjectContextService
{
    private readonly ProtectedSessionStorage _sessionStorage;
    private readonly IDataRetrievalService _dataService;
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly ILogger<ProjectContextService> _logger;
    private const string PROJECT_KEY = "CurrentProjectId";
    
    private Guid? _currentProjectId;
    private bool _isLoaded = false;
    private bool _isJsAvailable = false;

    public ProjectContextService(
        ProtectedSessionStorage sessionStorage,
        IDataRetrievalService dataService,
        AuthenticationStateProvider authenticationStateProvider,
        ILogger<ProjectContextService> logger)
    {
        _sessionStorage = sessionStorage;
        _dataService = dataService;
        _authenticationStateProvider = authenticationStateProvider;
        _logger = logger;
    }
    
    


    public Guid? CurrentProjectId => _currentProjectId;

    // public async Task<Project?> GetCurrentProjectAsync()
    // {
    //     await EnsureLoadedAsync();
    //     
    //     if (_currentProjectId == null)
    //         return null;
    //
    //     try
    //     {
    //         var result = await _dataService.ReadFromCacheOrDb(new Project());
    //         return result.Item1.FirstOrDefault(p => p.UID == _currentProjectId);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error getting current project");
    //         return null;
    //     }
    // }
    
    
  public async Task<Project?> GetCurrentProjectAsync()
{
    await EnsureLoadedAsync();
    
    if (_currentProjectId == null)
        return null;

    try
    {
        // Get the cached/stored project
        var result = await _dataService.ReadFromCacheOrDb<Project>();
        var cachedProject = result.Item1.FirstOrDefault(p => p.UID == _currentProjectId);
        
        if (cachedProject == null)
        {
            _logger.LogWarning($"Project {_currentProjectId} not found in database. Clearing cache.");
            await ClearCurrentProjectAsync();
            return null;
        }
        
        // VALIDATE: Check if user actually has access to this project
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        var userId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (!string.IsNullOrEmpty(userId))
        {
            var userProjects = await GetUserProjectsAsync(userId);
            var hasAccess = userProjects.Any(p => p.UID == _currentProjectId);
            
            if (!hasAccess)
            {
                _logger.LogWarning($"User {userId} does not have access to cached project {cachedProject.Tag} ({_currentProjectId}). Clearing and reassigning.");
                
                // Clear the invalid cached project
                await ClearCurrentProjectAsync();
                
                // Set to user's first available project
                if (userProjects.Any())
                {
                    // Prioritize projects where user is ProjectAdmin
                    var assignmentsResult = await _dataService.ReadFromCacheOrDb<ProjectUserAssignment>();
                    var userAssignments = assignmentsResult.Item1
                        .Where(pa => pa.UserId == userId && pa.IsActive)
                        .ToList();
                    
                    Project? defaultProject = null;
                    
                    // Try to find a project where user is admin
                    foreach (var project in userProjects)
                    {
                        var assignment = userAssignments.FirstOrDefault(pa => pa.ProjectId == project.UID);
                        if (assignment?.IsProjectAdmin == true)
                        {
                            defaultProject = project;
                            _logger.LogInformation($"Found admin project for user: {project.Tag}");
                            break;
                        }
                    }
                    
                    // If no admin project, use first project alphabetically
                    if (defaultProject == null)
                    {
                        defaultProject = userProjects.OrderBy(p => p.Tag).First();
                        _logger.LogInformation($"No admin project found, using first project: {defaultProject.Tag}");
                    }
                    
                    await SetCurrentProjectAsync(defaultProject.UID);
                    _logger.LogInformation($"Auto-assigned user {userId} to project: {defaultProject.Tag}");
                    return defaultProject;
                }
                
                _logger.LogWarning($"User {userId} has no project assignments");
                return null;
            }
            
            _logger.LogInformation($"User {userId} validated access to project {cachedProject.Tag}");
        }
        
        return cachedProject;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting current project");
        return null;
    }
}
    

    public async Task SetCurrentProjectAsync(Guid projectId)
    {
        _currentProjectId = projectId;
        _isLoaded = true;

        try
        {
            await _sessionStorage.SetAsync(PROJECT_KEY, projectId);
            _isJsAvailable = true;
            _logger.LogInformation($"Current project set to: {projectId}");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop"))
        {
            // JS not available yet (prerendering), cache value only
            _isJsAvailable = false;
            _logger.LogWarning("Cannot save to session storage during prerendering. Project ID cached in memory.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting current project");
        }
    }

    public async Task ClearCurrentProjectAsync()
    {
        _currentProjectId = null;
        _isLoaded = true;

        try
        {
            await _sessionStorage.DeleteAsync(PROJECT_KEY);
            _logger.LogInformation("Current project cleared");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop"))
        {
            // JS not available yet (prerendering)
            _isJsAvailable = false;
            _logger.LogWarning("Cannot clear session storage during prerendering. Project ID cleared from memory.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing current project");
        }
    }

    public async Task<List<Project>> GetUserProjectsAsync(string userId)
    {
        try
        {
            // Get all project assignments for this user
            var assignments = await _dataService.ReadFromCacheOrDb<ProjectUserAssignment>();
            var userAssignments = assignments.Item1
                .Where(a => a.UserId == userId && a.IsActive)
                .ToList();

            if (!userAssignments.Any())
                return new List<Project>();

            // Get all projects
            var projects = await _dataService.ReadFromCacheOrDb<Project>();
            
            // Filter to user's assigned projects
            var userProjectIds = userAssignments.Select(a => a.ProjectId).Distinct();
            return projects.Item1
                .Where(p => userProjectIds.Contains(p.UID) && p.Display)
                .OrderBy(p => p.Order)
                .ThenBy(p => p.Tag)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user projects");
            return new List<Project>();
        }
    }

    public async Task<bool> IsUserAdminInCurrentProjectAsync(string userId)
    {
        try
        {
            // Log the current project
            var currentProjectId = _currentProjectId;
            //_logger.LogInformation($"IsUserAdminInCurrentProjectAsync - CurrentProjectId: {currentProjectId}, UserId: {userId}");
        
            // Try to load project if not loaded
            if (currentProjectId == null)
            {
                await EnsureLoadedAsync();
                currentProjectId = _currentProjectId;
                //_logger.LogInformation($"IsUserAdminInCurrentProjectAsync - After EnsureLoaded, CurrentProjectId: {currentProjectId}");
            }
        
            if (currentProjectId == null)
            {
                //_logger.LogWarning("IsUserAdminInCurrentProjectAsync - No current project set");
                return false;
            }

            return await IsUserAdminInProjectAsync(userId, currentProjectId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in IsUserAdminInCurrentProjectAsync");
            return false;
        }
    }

    public async Task<bool> IsUserAdminInProjectAsync(string userId, Guid projectId)
    {
        try
        {
            var assignments = await _dataService.ReadFromCacheOrDb<ProjectUserAssignment>();
        
            //_logger.LogInformation($"IsUserAdminInProjectAsync - Total assignments: {assignments.Item1.Count}");
            //_logger.LogInformation($"IsUserAdminInProjectAsync - Looking for UserId: {userId}, ProjectId: {projectId}");
        
            // Log all assignments for this user
            var userAssignments = assignments.Item1.Where(a => a.UserId == userId).ToList();
            //_logger.LogInformation($"IsUserAdminInProjectAsync - Found {userAssignments.Count} assignments for user");
        
            foreach (var assignment in userAssignments)
            {
                //_logger.LogInformation($"  Assignment: ProjectId={assignment.ProjectId}, IsProjectAdmin={assignment.IsProjectAdmin}, IsActive={assignment.IsActive}");
            }
        
            var result = assignments.Item1.Any(a => 
                a.UserId == userId && 
                a.ProjectId == projectId && 
                a.IsProjectAdmin && 
                a.IsActive);
            
            //_logger.LogInformation($"IsUserAdminInProjectAsync - Result: {result}");
        
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user is admin in project");
            return false;
        }
    }

    public async Task<List<string>> GetUserCustomRolesInCurrentProjectAsync(string userId)
    {
        await EnsureLoadedAsync();
        
        if (_currentProjectId == null)
            return new List<string>();

        try
        {
            var userRoles = await _dataService.ReadFromCacheOrDb<ProjectUserRole>();
            return userRoles.Item1
                .Where(r => r.UserId == userId && 
                           r.ProjectId == _currentProjectId && 
                           r.IsActive)
                .Select(r => r.CustomRoleName)
                .Distinct()
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user custom roles");
            return new List<string>();
        }
    }

    public async Task<List<string>> GetUserHardRolesInCurrentProjectAsync(string userId)
    {
        try
        {
            var currentProjectId = _currentProjectId;
        
            // Try to load project if not loaded
            if (currentProjectId == null)
            {
                await EnsureLoadedAsync();
                currentProjectId = _currentProjectId;
            }
        
            if (currentProjectId == null)
            {
                _logger.LogWarning($"GetUserHardRoles: No current project for user {userId}");
                return new List<string>();
            }

            return await GetUserHardRolesInProjectAsync(userId, currentProjectId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting hard roles for user {userId}");
            return new List<string>();
        }
    }
    
public async Task<List<string>> GetUserHardRolesInProjectAsync(string userId, Guid projectId)
{
    try
    {
        _logger.LogInformation($"GetUserHardRolesInProject - UserId: {userId}, ProjectId: {projectId}");
        
        // Get user's role assignments from ProjectUserRole table
        var userRoleAssignments = await _dataService.ReadFromCacheOrDb<ProjectUserRole>();
        
        _logger.LogInformation($"GetUserHardRoles: Total ProjectUserRole records: {userRoleAssignments.Item1.Count}");
        
        var assignments = userRoleAssignments.Item1
            .Where(a => a.UserId == userId && 
                       a.ProjectId == projectId &&
                       a.IsActive)
            .ToList();

        _logger.LogInformation($"GetUserHardRoles: Found {assignments.Count} role assignments for user {userId} in project {projectId}");

        if (!assignments.Any())
        {
            _logger.LogWarning($"GetUserHardRoles: No role assignments found");
            return new List<string>();
        }

        // Get the role mappings for this project
        var roleMappings = await _dataService.ReadFromCacheOrDb<RoleMapping>();
        
        var projectMappings = roleMappings.Item1
            .Where(rm => rm.ProjectId == projectId && rm.IsActive)
            .ToList();
        
        _logger.LogInformation($"GetUserHardRoles: Found {projectMappings.Count} role mappings for project");
        
        var hardRoles = new HashSet<string>();
        
        foreach (var assignment in assignments)
        {
            _logger.LogInformation($"GetUserHardRoles: Processing CustomRoleName: {assignment.CustomRoleName}");
            
            // MATCH BY CUSTOM ROLE NAME
            var roleMapping = projectMappings.FirstOrDefault(rm => 
                rm.CustomRoleName.Equals(assignment.CustomRoleName, StringComparison.OrdinalIgnoreCase));
            
            if (roleMapping != null && !string.IsNullOrEmpty(roleMapping.MappedHardRoles))
            {
                try
                {
                    var mapped = System.Text.Json.JsonSerializer.Deserialize<List<string>>(roleMapping.MappedHardRoles);
                    
                    if (mapped != null && mapped.Any())
                    {
                        foreach (var role in mapped)
                        {
                            hardRoles.Add(role);
                        }
                        
                        _logger.LogInformation($"GetUserHardRoles: Role '{assignment.CustomRoleName}' adds hard roles: {string.Join(", ", mapped)}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error deserializing roles for {assignment.CustomRoleName}");
                }
            }
            else
            {
                _logger.LogWarning($"GetUserHardRoles: No role mapping found for CustomRoleName: {assignment.CustomRoleName}");
            }
        }

        var distinctRoles = hardRoles.ToList();
        _logger.LogInformation($"GetUserHardRoles: Final hard roles: {string.Join(", ", distinctRoles)}");
        
        return distinctRoles;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Error getting hard roles for user {userId} in project {projectId}");
        return new List<string>();
    }
}
    
    

    private async Task EnsureLoadedAsync()
    {
        if (_isLoaded)
            return;

        try
        {
            var result = await _sessionStorage.GetAsync<Guid>(PROJECT_KEY);
            if (result.Success)
            {
                _currentProjectId = result.Value;
            }
            _isLoaded = true;
            _isJsAvailable = true;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop"))
        {
            // JS not available yet (prerendering)
            // Use cached value if available, otherwise return without loading
            _isJsAvailable = false;
            _isLoaded = false; // Will retry later when JS is available
            _logger.LogWarning("Cannot access session storage during prerendering. Will retry after render.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading current project from session");
            _isLoaded = true; // Prevent infinite retry
        }
    }


    
}