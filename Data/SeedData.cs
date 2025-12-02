using System.Collections.Generic;
using System.Threading.Tasks;

namespace ElDesignApp.Data;

using Microsoft.AspNetCore.Identity;
using ElDesignApp.Middleware;


public static class SeedData
{
    public static async Task InitializeAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
        //SignInManager<ApplicationUser> signInManager)
    {
        string seedEmail = "admin@eldesign.app";
        string seedUserName = "admin";
        string seedPassword = "!Useless@123";
        
        List<string> rolesToAssign = ["SuperAdmin", "Admin", "User"];
        
        // Create Admin and SuperAdmin role if it doesn't exist
        foreach (var roleName in rolesToAssign)
        {
            if (await roleManager.FindByNameAsync(roleName) == null)
            {
                // If they are not in the role, add them
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // Create the admin user if it doesn't exist
        //var user = await userManager.FindByEmailAsync(seedEmail);
        var user = await userManager.FindByNameAsync(seedUserName);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = seedUserName,
                Email = seedEmail,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(user, seedPassword);
            if (createResult.Succeeded)
            {
                await userManager.AddToRolesAsync(user, rolesToAssign);
            }
        }
        else
        {
            // If the user already exists but has no role, add it
            foreach (var roleName in rolesToAssign)
            {
                // Check if the user is ALREADY in the current role
                if (!await userManager.IsInRoleAsync(user, roleName))
                {
                    // If they are not in the role, add them
                    await userManager.AddToRoleAsync(user, roleName);
                }
            }
        }
        
    }
}