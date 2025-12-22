namespace ElDesignApp.Constants;

/// <summary>
/// Hard-coded system roles for authorization
/// These are the actual roles checked by authorization policies
/// </summary>
public static class HardRoles
{
    /// <summary>
    /// Super Administrator - God mode, can do anything across all projects
    /// </summary>
    public const string SuperAdmin = "SuperAdmin";
    
    /// <summary>
    /// Project Administrator - Can manage users and settings within assigned projects
    /// </summary>
    public const string Admin = "Admin";
    
    /// <summary>
    /// User - Can perform calculations and modify data within assigned projects
    /// </summary>
    public const string User = "User";
    
    /// <summary>
    /// Report - Read-only access, can only generate and view reports
    /// </summary>
    public const string Report = "Report";
    
    /// <summary>
    /// Guest - Limited trial access with restricted functionality
    /// </summary>
    public const string Guest = "Guest";
    
    /// <summary>
    /// Get all hard role names as a list
    /// </summary>
    public static List<string> GetAll() => new()
    {
        SuperAdmin,
        Admin,
        User,
        Report,
        Guest
    };
    
    /// <summary>
    /// Check if a role name is a valid hard role
    /// </summary>
    public static bool IsValidRole(string role) => GetAll().Contains(role);
    
    /// <summary>
    /// Get display-friendly description for a role
    /// </summary>
    public static string GetDescription(string role) => role switch
    {
        SuperAdmin => "Super Administrator - Full system access",
        Admin => "Project Administrator - Manage project users and settings",
        User => "User - Perform calculations and modify data",
        Report => "Report Viewer - Read-only, generate reports",
        Guest => "Guest - Limited trial access",
        _ => "Unknown Role"
    };
}