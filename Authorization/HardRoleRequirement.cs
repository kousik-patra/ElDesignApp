using Microsoft.AspNetCore.Authorization;

namespace ElDesignApp.Authorization;

public class HardRoleRequirement : IAuthorizationRequirement
{
    public string[] HardRoles { get; }

    public HardRoleRequirement(params string[] hardRoles)
    {
        HardRoles = hardRoles;
    }
}