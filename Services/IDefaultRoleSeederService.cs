using ElDesignApp.Models;
using ElDesignApp.Services.DataBase;

namespace ElDesignApp.Services;

public interface IDefaultRoleSeederService
{
    Task SeedDefaultRolesForProjectAsync(Guid projectId);
}

public class DefaultRoleSeederService : IDefaultRoleSeederService
{
    private readonly ITableService _tableService;
    private readonly ILogger<DefaultRoleSeederService> _logger;

    public DefaultRoleSeederService(
        ITableService tableService,
        ILogger<DefaultRoleSeederService> logger)
    {
        _tableService = tableService;
        _logger = logger;
    }

    public async Task SeedDefaultRolesForProjectAsync(Guid projectId)
    {
        try
        {
            _logger.LogInformation($"Seeding default roles for project {projectId}");

            // Check if roles already exist for this project
            var existingRoles = await _tableService.GetListAsync(new RoleMapping(), projectId.ToString()) ?? new();
            var existingRoleNames = existingRoles
                .Where(r => r.ProjectId == projectId && r.IsActive)
                .Select(r => r.CustomRoleName)
                .ToList();

            // Define default roles
            var defaultRoles = new List<(string Name, string Description, List<string> HardRoles)>
            {
                ("Engineer", "Engineering staff with standard user access", ["User"]),
                ("Manager", "Project Manager with admin rights", ["Admin", "Report"]),
                ("Designer", "Design team member with user access", ["User"]),
                ("ProjectAdmin", "Project Administrator with full project control", ["Admin"]),
                ("Viewer", "Read-only access to project data", ["Report"])
            };

            var rolesCreated = 0;

            foreach (var (name, description, hardRoles) in defaultRoles)
            {
                // Skip if role already exists
                if (existingRoleNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                {
                    _logger.LogInformation($"Role '{name}' already exists for project {projectId}, skipping");
                    continue;
                }

                var newRole = new RoleMapping
                {
                    UID = Guid.NewGuid(),
                    ProjectId = projectId,
                    CustomRoleName = name,
                    Description = description,
                    MappedHardRoles = System.Text.Json.JsonSerializer.Serialize(hardRoles),
                    IsActive = true,
                    CreatedOn = DateTime.Now,
                    CreatedBy = "System"
                };

                await _tableService.InsertItemAsync(newRole);
                rolesCreated++;
                _logger.LogInformation($"Created default role '{name}' for project {projectId}");
            }

            _logger.LogInformation($"Seeded {rolesCreated} default roles for project {projectId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error seeding default roles for project {projectId}");
            throw;
        }
    }
}