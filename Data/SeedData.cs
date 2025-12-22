using System.Collections.Generic;
using System.Threading.Tasks;

namespace ElDesignApp.Data;

using Microsoft.AspNetCore.Identity;
using ElDesignApp.Middleware;
using ElDesignApp.Constants;

public static class SeedData
{
    public static async Task InitializeAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
        //SignInManager<ApplicationUser> signInManager)
    {
        // Create all hard roles if they don't exist
        foreach (var roleName in HardRoles.GetAll())
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
        
        // Create SuperAdmin user
        var superAdminEmail = "admin@eldesignapp.com";
        var superAdmin = await userManager.FindByEmailAsync(superAdminEmail);
        
        if (superAdmin == null)
        {
            superAdmin = new ApplicationUser
            {
                UserName = "admin",
                Email = superAdminEmail,
                EmailConfirmed = true
            };
            
            await userManager.CreateAsync(superAdmin, "!Useless@123");
            await userManager.AddToRoleAsync(superAdmin, HardRoles.SuperAdmin);
        }
        
        
        
        
    }
}