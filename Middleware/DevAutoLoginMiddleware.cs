// Middleware/DevAutoLoginMiddleware.cs
namespace ElDesignApp.Middleware;

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using ElDesignApp.Data;
using Microsoft.AspNetCore.Http; // ← your ApplicationUser namespace

public class DevAutoLoginMiddleware
{
    private readonly RequestDelegate _next;

    // Dev-only credentials
    private const string DevEmail = "admin@eldesign.app";
    private const string DevUserName = "admin";
    private const string DevPassword = "!Useless@123";
    private static readonly string[] DevRoles = [ "SuperAdmin", "Admin", "User" ];

    public DevAutoLoginMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        // Only run in Development + not already authenticated
        if (!context.Request.Host.Host.Contains("localhost", StringComparison.OrdinalIgnoreCase) ||
            context.User.Identity?.IsAuthenticated == true)
        {
            await _next(context);
            return;
        }

        // Create or get the dev user (in memory if possible, but we have to use UserManager)
        var user = await userManager.FindByEmailAsync(DevEmail);

        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = DevUserName,
                Email = DevEmail,
                EmailConfirmed = true,
                LockoutEnabled = false
            };

            var createResult = await userManager.CreateAsync(user, DevPassword);
            if (!createResult.Succeeded)
            {
                // If create fails (rare), just continue without login
                await _next(context);
                return;
            }

            // Add to all roles
            foreach (var role in DevRoles)
            {
                if (!await userManager.IsInRoleAsync(user, role))
                    await userManager.AddToRoleAsync(user, role);
            }
        }

        // AUTO LOGIN
        await signInManager.SignInAsync(user, isPersistent: false);

        await _next(context);
    }
}