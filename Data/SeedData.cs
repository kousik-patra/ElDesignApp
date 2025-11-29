namespace ElDesignApp.Data;

using Microsoft.AspNetCore.Identity;

public static class SeedData
{
    public static async Task InitializeAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        string seedEmail = "admin@myapp.com";
        string seedUserName = "admin";
        string seedPassword = "Admin@123";
        

        // Create Admin and SuperAdmin role if it doesn't exist
        if (await roleManager.FindByNameAsync("SuperAdmin") == null)
        {
            await roleManager.CreateAsync(new IdentityRole("SuperAdmin"));
        }
        if (await roleManager.FindByNameAsync("Admin") == null)
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
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
                await userManager.AddToRoleAsync(user, "Admin");
                await userManager.AddToRoleAsync(user, "SuperAdmin");
            }
        }
        else
        {
            // If the user already exists but has no role, add it
            if (!await userManager.IsInRoleAsync(user, "Admin"))
            {
                await userManager.AddToRoleAsync(user, "Admin");
            }
            if (!await userManager.IsInRoleAsync(user, "SuperAdmin"))
            {
                await userManager.AddToRoleAsync(user, "SuperAdmin");
            }
        }
    }
}