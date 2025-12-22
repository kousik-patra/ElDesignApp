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
    Task AssignProjectRoleAsync(string adminUserId, Guid projectId, string targetUserId, string[] roles);
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
            
            // Check if user is Project Admin
            var projects = await _dataService.ReadFromCacheOrDb(new Project());
            var project = projects.Item1.FirstOrDefault(p => p.UID == projectId);
            
            if (project != null && role == HardRoles.Admin)
            {
                var admins = JsonSerializer.Deserialize<List<string>>(project.ProjectAdmins) ?? new List<string>();
                if (admins.Contains(userId))
                    return true;
            }
            
            // Check ProjectUserAssignment using LoadData
            var sql = "SELECT * FROM ProjectUserAssignment WHERE ProjectId = @ProjectId AND UserId = @UserId AND IsActive = 1";
            var assignments = await _tableService.LoadData<ProjectUserAssignment, dynamic>(sql, 
                new { ProjectId = projectId, UserId = userId });
            
            var assignment = assignments?.FirstOrDefault();
            if (assignment != null)
            {
                var projectRoles = JsonSerializer.Deserialize<List<string>>(assignment.ProjectRoles) ?? new List<string>();
                return projectRoles.Contains(role);
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
            var projects = await _dataService.ReadFromCacheOrDb(new Project());
            
            // SuperAdmin sees all projects
            if (await IsInHardRoleAsync(userId, HardRoles.SuperAdmin))
            {
                return projects.Item1.Where(p => p.Display).ToList();
            }
            
            // Regular admins see only their assigned projects
            return projects.Item1.Where(p =>
            {
                var admins = JsonSerializer.Deserialize<List<string>>(p.ProjectAdmins ?? "[]") ?? new List<string>();
                return admins.Contains(userId) && p.Display;
            }).ToList();
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
            // Get projects where user is Admin
            var adminProjects = await GetAdminProjectsAsync(userId);
            
            // Get projects where user has assignments using LoadData
            var sql = "SELECT DISTINCT ProjectId FROM ProjectUserAssignment WHERE UserId = @UserId AND IsActive = 1";
            var projectIdRecords = await _tableService.LoadData<ProjectIdRecord, dynamic>(sql, new { UserId = userId });
            
            if (projectIdRecords != null && projectIdRecords.Any())
            {
                var projectIds = projectIdRecords.Select(p => p.ProjectId).ToList();
                var allProjects = await _dataService.ReadFromCacheOrDb(new Project());
                var assignedProjects = allProjects.Item1.Where(p => projectIds.Contains(p.UID) && p.Display).ToList();
                
                // Combine and remove duplicates
                return adminProjects.Union(assignedProjects).DistinctBy(p => p.UID).ToList();
            }
            
            return adminProjects;
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
            var projects = await _dataService.ReadFromCacheOrDb(new Project());
            var project = projects.Item1.FirstOrDefault(p => p.UID == projectId);
            
            if (project != null)
            {
                var admins = JsonSerializer.Deserialize<List<string>>(project.ProjectAdmins ?? "[]") ?? new List<string>();
                if (admins.Contains(userId))
                {
                    roles.Add(HardRoles.Admin);
                }
            }
            
            // Check ProjectUserAssignment using LoadData
            var sql = "SELECT * FROM ProjectUserAssignment WHERE ProjectId = @ProjectId AND UserId = @UserId AND IsActive = 1";
            var assignments = await _tableService.LoadData<ProjectUserAssignment, dynamic>(sql, 
                new { ProjectId = projectId, UserId = userId });
            
            var assignment = assignments?.FirstOrDefault();
            if (assignment != null)
            {
                var projectRoles = JsonSerializer.Deserialize<List<string>>(assignment.ProjectRoles ?? "[]") ?? new List<string>();
                foreach (var role in projectRoles)
                {
                    roles.Add(role);
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
            
            var projects = await _dataService.ReadFromCacheOrDb(new Project());
            var project = projects.Item1.FirstOrDefault(p => p.UID == projectId);
            
            if (project == null)
                return new List<ApplicationUser>();
            
            var softAssignedUserIds = JsonSerializer.Deserialize<List<string>>(project.SoftAssignedUsers ?? "[]") ?? new List<string>();
            
            var users = new List<ApplicationUser>();
            foreach (var userId in softAssignedUserIds)
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    users.Add(user);
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

    public async Task AssignProjectRoleAsync(string adminUserId, Guid projectId, string targetUserId, string[] roles)
    {
        try
        {
            // Verify admin has permission
            var isAdmin = await HasProjectRoleAsync(adminUserId, projectId, HardRoles.Admin);
            if (!isAdmin)
            {
                throw new UnauthorizedAccessException("User is not an admin of this project");
            }
            
            // Check if assignment exists
            var sql = "SELECT * FROM ProjectUserAssignment WHERE ProjectId = @ProjectId AND UserId = @UserId";
            var existingAssignments = await _tableService.LoadData<ProjectUserAssignment, dynamic>(sql, 
                new { ProjectId = projectId, UserId = targetUserId });
            
            var existing = existingAssignments?.FirstOrDefault(a => a.IsActive);
            
            if (existing != null)
            {
                // Update existing
                existing.ProjectRoles = JsonSerializer.Serialize(roles);
                await _tableService.UpdateParameter(existing, existing.UID, new List<string> { "ProjectRoles" });
            }
            else
            {
                // Create new
                var assignment = new ProjectUserAssignment
                {
                    UID = Guid.NewGuid(),
                    ProjectId = projectId,
                    UserId = targetUserId,
                    ProjectRoles = JsonSerializer.Serialize(roles),
                    IsActive = true,
                    AssignedBy = adminUserId,
                    AssignedOn = DateTime.Now
                };
                
                await _tableService.InsertItemAsync(assignment);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning roles to user {UserId} in project {ProjectId}", targetUserId, projectId);
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