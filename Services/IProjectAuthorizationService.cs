// Services/ProjectAuthorizationService.cs
using System.Text.Json;
using ElDesignApp.Constants;
using ElDesignApp.Data;
using ElDesignApp.Models;
using Microsoft.AspNetCore.Identity;

namespace ElDesignApp.Services;

public interface IProjectAuthorizationService
{
    Task<bool> IsInHardRoleAsync(string userId, string hardRole);
    Task<bool> HasProjectRoleAsync(string userId, Guid projectId, string role);
    Task<List<Project>> GetAdminProjectsAsync(string userId);
    Task<List<Project>> GetUserProjectsAsync(string userId);
    Task<List<string>> GetUserProjectRolesAsync(string userId, Guid projectId);
    Task<List<ApplicationUser>> GetSoftAssignedUsersAsync(string adminUserId, Guid projectId);
    Task<List<ApplicationUser>> GetProjectAssignedUsersAsync(string adminUserId, Guid projectId);
    Task AssignProjectRoleAsync(string adminUserId, Guid projectId, string targetUserId, string customRoleName);
    Task RemoveProjectAssignmentAsync(string adminUserId, Guid projectId, string targetUserId);
    Task<List<ProjectUserAssignment>> GetProjectAssignmentsAsync(Guid projectId);
}

public class ProjectAuthorizationService : IProjectAuthorizationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDataRetrievalService _dataService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMyTableService _tableService;
    private readonly ILogger<ProjectAuthorizationService> _logger;

    public ProjectAuthorizationService(
        IServiceProvider serviceProvider,
        IDataRetrievalService dataService,
        UserManager<ApplicationUser> userManager,
        IMyTableService tableService,
        ILogger<ProjectAuthorizationService> logger)
    {
        _serviceProvider = serviceProvider;
        _dataService = dataService;
        _userManager = userManager;
        _tableService = tableService;
        _logger = logger;
    }

    public async Task<bool> IsInHardRoleAsync(string userId, string hardRole)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;
            
            var roles = await _userManager.GetRolesAsync(user);
            
            // SuperAdmin can assume any role
            if (roles.Contains(HardRoles.SuperAdmin))
                return true;
                
            return roles.Contains(hardRole);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user {UserId} is in role {Role}", userId, hardRole);
            return false;
        }
    }

    public async Task<bool> HasProjectRoleAsync(string userId, Guid projectId, string role)
    {
        try
        {
            // SuperAdmin has all roles everywhere
            if (await IsInHardRoleAsync(userId, HardRoles.SuperAdmin))
                return true;
            
            // Check if user is Project Admin via ProjectUserAssignment
            var sql = "SELECT * FROM ProjectUserAssignment WHERE ProjectId = @ProjectId AND UserId = @UserId AND IsActive = 1";
            var assignments = await _tableService.LoadData<ProjectUserAssignment, dynamic>(sql, 
                new { ProjectId = projectId, UserId = userId });
            
            var assignment = assignments?.FirstOrDefault();
            if (assignment != null)
            {
                // If checking for Admin role and user is ProjectAdmin
                if (role == HardRoles.Admin && assignment.IsProjectAdmin)
                    return true;
                
                // Get user's custom roles in this project
                var roleSql = "SELECT CustomRoleName FROM ProjectUserRoles WHERE ProjectId = @ProjectId AND UserId = @UserId AND IsActive = 1";
                var userRoles = await _tableService.LoadData<CustomRoleRecord, dynamic>(roleSql, 
                    new { ProjectId = projectId, UserId = userId });
                
                if (userRoles != null && userRoles.Any())
                {
                    // Get role mappings for this project
                    var mappingSql = "SELECT * FROM RoleMapping WHERE ProjectId = @ProjectId AND IsActive = 1";
                    var mappings = await _tableService.LoadData<RoleMapping, dynamic>(mappingSql, 
                        new { ProjectId = projectId });
                    
                    // Check if any of user's custom roles map to the required hard role
                    foreach (var userRole in userRoles)
                    {
                        var mapping = mappings?.FirstOrDefault(m => m.CustomRoleName == userRole.CustomRoleName);
                        if (mapping != null)
                        {
                            var hardRoles = JsonSerializer.Deserialize<List<string>>(mapping.MappedHardRoles) ?? new List<string>();
                            if (hardRoles.Contains(role))
                                return true;
                        }
                    }
                }
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking project role for user {UserId} in project {ProjectId}", userId, projectId);
            return false;
        }
    }

    public async Task<List<Project>> GetAdminProjectsAsync(string userId)
    {
        try
        {
            var projects = await _dataService.ReadFromCacheOrDb<Project>();
            
            // SuperAdmin sees all projects
            if (await IsInHardRoleAsync(userId, HardRoles.SuperAdmin))
            {
                return projects.Item1.Where(p => p.Display).ToList();
            }
            
            // Get projects where user is ProjectAdmin
            var sql = "SELECT ProjectId FROM ProjectUserAssignment WHERE UserId = @UserId AND IsProjectAdmin = 1 AND IsActive = 1";
            var adminProjectIds = await _tableService.LoadData<ProjectIdRecord, dynamic>(sql, new { UserId = userId });
            
            if (adminProjectIds != null && adminProjectIds.Any())
            {
                var projectIds = adminProjectIds.Select(p => p.ProjectId).ToHashSet();
                return projects.Item1.Where(p => projectIds.Contains(p.UID) && p.Display).ToList();
            }
            
            return new List<Project>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting admin projects for user {UserId}", userId);
            return new List<Project>();
        }
    }

    public async Task<List<Project>> GetUserProjectsAsync(string userId)
    {
        try
        {
            // Get all projects where user has any assignment
            var sql = "SELECT DISTINCT ProjectId FROM ProjectUserAssignment WHERE UserId = @UserId AND IsActive = 1";
            var projectIdRecords = await _tableService.LoadData<ProjectIdRecord, dynamic>(sql, new { UserId = userId });
            
            if (projectIdRecords != null && projectIdRecords.Any())
            {
                var projectIds = projectIdRecords.Select(p => p.ProjectId).ToList();
                var allProjects = await _dataService.ReadFromCacheOrDb<Project>();
                return allProjects.Item1.Where(p => projectIds.Contains(p.UID) && p.Display).ToList();
            }
            
            return new List<Project>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user projects for user {UserId}", userId);
            return new List<Project>();
        }
    }

    public async Task<List<string>> GetUserProjectRolesAsync(string userId, Guid projectId)
    {
        try
        {
            var roles = new HashSet<string>();
            
            // SuperAdmin has all roles
            if (await IsInHardRoleAsync(userId, HardRoles.SuperAdmin))
            {
                return new List<string> { HardRoles.SuperAdmin, HardRoles.Admin, HardRoles.User, HardRoles.Report };
            }
            
            // Check if Project Admin
            var assignmentSql = "SELECT * FROM ProjectUserAssignment WHERE ProjectId = @ProjectId AND UserId = @UserId AND IsActive = 1";
            var assignments = await _tableService.LoadData<ProjectUserAssignment, dynamic>(assignmentSql, 
                new { ProjectId = projectId, UserId = userId });
            
            var assignment = assignments?.FirstOrDefault();
            if (assignment != null)
            {
                if (assignment.IsProjectAdmin)
                {
                    roles.Add(HardRoles.Admin);
                }
                
                // Get user's custom roles
                var roleSql = "SELECT CustomRoleName FROM ProjectUserRoles WHERE ProjectId = @ProjectId AND UserId = @UserId AND IsActive = 1";
                var userRoles = await _tableService.LoadData<CustomRoleRecord, dynamic>(roleSql, 
                    new { ProjectId = projectId, UserId = userId });
                
                if (userRoles != null && userRoles.Any())
                {
                    // Get role mappings
                    var mappingSql = "SELECT * FROM RoleMapping WHERE ProjectId = @ProjectId AND IsActive = 1";
                    var mappings = await _tableService.LoadData<RoleMapping, dynamic>(mappingSql, 
                        new { ProjectId = projectId });
                    
                    foreach (var userRole in userRoles)
                    {
                        var mapping = mappings?.FirstOrDefault(m => m.CustomRoleName == userRole.CustomRoleName);
                        if (mapping != null)
                        {
                            var hardRoles = JsonSerializer.Deserialize<List<string>>(mapping.MappedHardRoles) ?? new List<string>();
                            foreach (var hardRole in hardRoles)
                            {
                                roles.Add(hardRole);
                            }
                        }
                    }
                }
            }
            
            return roles.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user project roles for user {UserId} in project {ProjectId}", userId, projectId);
            return new List<string>();
        }
    }

    public async Task<List<ApplicationUser>> GetSoftAssignedUsersAsync(string adminUserId, Guid projectId)
    {
        try
        {
            // Verify admin has permission
            var isAdmin = await HasProjectRoleAsync(adminUserId, projectId, HardRoles.Admin);
            if (!isAdmin)
            {
                throw new UnauthorizedAccessException("User is not an admin of this project");
            }
            
            // Get all users assigned to this project who are NOT admins
            var sql = "SELECT UserId FROM ProjectUserAssignment WHERE ProjectId = @ProjectId AND IsProjectAdmin = 0 AND IsActive = 1";
            var userIdRecords = await _tableService.LoadData<UserIdRecord, dynamic>(sql, new { ProjectId = projectId });
            
            var users = new List<ApplicationUser>();
            if (userIdRecords != null)
            {
                foreach (var record in userIdRecords)
                {
                    var user = await _userManager.FindByIdAsync(record.UserId);
                    if (user != null)
                    {
                        users.Add(user);
                    }
                }
            }
            
            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting soft assigned users for project {ProjectId}", projectId);
            throw;
        }
    }

    public async Task<List<ApplicationUser>> GetProjectAssignedUsersAsync(string adminUserId, Guid projectId)
    {
        try
        {
            // Verify admin has permission
            var isAdmin = await HasProjectRoleAsync(adminUserId, projectId, HardRoles.Admin);
            if (!isAdmin)
            {
                throw new UnauthorizedAccessException("User is not an admin of this project");
            }
            
            // Get all active assignments for this project
            var sql = "SELECT DISTINCT UserId FROM ProjectUserAssignment WHERE ProjectId = @ProjectId AND IsActive = 1";
            var userIdRecords = await _tableService.LoadData<UserIdRecord, dynamic>(sql, new { ProjectId = projectId });
            
            var users = new List<ApplicationUser>();
            if (userIdRecords != null)
            {
                foreach (var record in userIdRecords)
                {
                    var user = await _userManager.FindByIdAsync(record.UserId);
                    if (user != null)
                    {
                        users.Add(user);
                    }
                }
            }
            
            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assigned users for project {ProjectId}", projectId);
            throw;
        }
    }

    public async Task AssignProjectRoleAsync(string adminUserId, Guid projectId, string targetUserId, string customRoleName)
    {
        try
        {
            // Verify admin has permission
            var isAdmin = await HasProjectRoleAsync(adminUserId, projectId, HardRoles.Admin);
            if (!isAdmin)
            {
                throw new UnauthorizedAccessException("User is not an admin of this project");
            }
            
            // Check if user is assigned to the project
            var assignmentCheckSql = "SELECT * FROM ProjectUserAssignment WHERE ProjectId = @ProjectId AND UserId = @UserId AND IsActive = 1";
            var existingAssignments = await _tableService.LoadData<ProjectUserAssignment, dynamic>(assignmentCheckSql, 
                new { ProjectId = projectId, UserId = targetUserId });
            
            if (existingAssignments == null || !existingAssignments.Any())
            {
                throw new InvalidOperationException("User must be assigned to the project before assigning roles");
            }
            
            // Check if role assignment already exists
            var roleCheckSql = "SELECT * FROM ProjectUserRoles WHERE ProjectId = @ProjectId AND UserId = @UserId AND CustomRoleName = @CustomRoleName";
            var existingRoles = await _tableService.LoadData<ProjectUserRole, dynamic>(roleCheckSql, 
                new { ProjectId = projectId, UserId = targetUserId, CustomRoleName = customRoleName });
            
            var existing = existingRoles?.FirstOrDefault(r => r.IsActive);
            
            if (existing != null)
            {
                // Already assigned
                return;
            }
            
            // Create new role assignment
            var roleAssignment = new ProjectUserRole
            {
                UID = Guid.NewGuid(),
                ProjectId = projectId,
                UserId = targetUserId,
                CustomRoleName = customRoleName,
                AssignedBy = adminUserId,
                AssignedOn = DateTime.Now,
                IsActive = true
            };
            
            await _tableService.InsertItemAsync(roleAssignment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role {Role} to user {UserId} in project {ProjectId}", customRoleName, targetUserId, projectId);
            throw;
        }
    }

    public async Task RemoveProjectAssignmentAsync(string adminUserId, Guid projectId, string targetUserId)
    {
        try
        {
            // Verify admin has permission
            var isAdmin = await HasProjectRoleAsync(adminUserId, projectId, HardRoles.Admin);
            if (!isAdmin)
            {
                throw new UnauthorizedAccessException("User is not an admin of this project");
            }
            
            var sql = "SELECT * FROM ProjectUserAssignment WHERE ProjectId = @ProjectId AND UserId = @UserId AND IsActive = 1";
            var assignments = await _tableService.LoadData<ProjectUserAssignment, dynamic>(sql, 
                new { ProjectId = projectId, UserId = targetUserId });
            
            var assignment = assignments?.FirstOrDefault();
            if (assignment != null)
            {
                assignment.IsActive = false;
                assignment.RemovedBy = adminUserId;
                assignment.RemovedOn = DateTime.Now;
                
                await _tableService.UpdateParameter(assignment, assignment.UID, 
                    new List<string> { "IsActive", "RemovedBy", "RemovedOn" });
                    
                // Also remove all role assignments
                var roleSql = "SELECT * FROM ProjectUserRoles WHERE ProjectId = @ProjectId AND UserId = @UserId AND IsActive = 1";
                var roleAssignments = await _tableService.LoadData<ProjectUserRole, dynamic>(roleSql, 
                    new { ProjectId = projectId, UserId = targetUserId });
                
                if (roleAssignments != null)
                {
                    foreach (var roleAssignment in roleAssignments)
                    {
                        roleAssignment.IsActive = false;
                        roleAssignment.RemovedBy = adminUserId;
                        roleAssignment.RemovedOn = DateTime.Now;
                        
                        await _tableService.UpdateParameter(roleAssignment, roleAssignment.UID, 
                            new List<string> { "IsActive", "RemovedBy", "RemovedOn" });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing assignment for user {UserId} from project {ProjectId}", targetUserId, projectId);
            throw;
        }
    }

    public async Task<List<ProjectUserAssignment>> GetProjectAssignmentsAsync(Guid projectId)
    {
        try
        {
            var sql = "SELECT * FROM ProjectUserAssignment WHERE ProjectId = @ProjectId AND IsActive = 1";
            var assignments = await _tableService.LoadData<ProjectUserAssignment, dynamic>(sql, new { ProjectId = projectId });
            return assignments ?? new List<ProjectUserAssignment>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assignments for project {ProjectId}", projectId);
            return new List<ProjectUserAssignment>();
        }
    }
}

// Helper classes for LoadData returns
public class ProjectIdRecord
{
    public Guid ProjectId { get; set; }
}

public class UserIdRecord
{
    public string UserId { get; set; } = string.Empty;
}

public class CustomRoleRecord
{
    public string CustomRoleName { get; set; } = string.Empty;
}