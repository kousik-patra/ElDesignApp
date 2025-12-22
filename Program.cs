using System;
using System.Data;
using ElDesignApp.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ElDesignApp.Components;
using ElDesignApp.Components.Account;
using ElDesignApp.Data;
using ElDesignApp.Middleware;
using ElDesignApp.Services;
using ElDesignApp.Services.Cache;
using ElDesignApp.Services.DataBase;
using ElDesignApp.Services.Global;
using ElDesignApp.Services.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Resend;
using ElDesignApp.Constants;
using OfficeOpenXml;
using ICacheService = ElDesignApp.Services.Cache.ICacheService;
using IGlobalDataService = ElDesignApp.Services.Global.IGlobalDataService;


var builder = WebApplication.CreateBuilder(args);


ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    //options.UseSqlite(connectionString));
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequiredLength = 6;
        options.Password.RequiredUniqueChars = 1;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

builder.Services.AddDistributedMemoryCache();

// Add custom services
    builder.Services.Configure<ConnectionStrings>(builder.Configuration.GetSection("ConnectionStrings"));
    builder.Services.AddScoped<IMiscService, MiscService>();
    builder.Services.AddScoped<IDbConnectionFactory, SqlConnectionFactory>();
    builder.Services.AddScoped<IMyTableService, MyTableService>();
    builder.Services.AddScoped<ITableService, TableService>();
    builder.Services.AddScoped<IDataRetrievalService, DataRetrievalService>();
    builder.Services.AddScoped<IMyFunctionService, MyFunctionService>();
    builder.Services.AddScoped<ILayoutFunctionService, LayoutFunctionService>();
    builder.Services.AddScoped<ICacheService, CacheService>();
    builder.Services.AddScoped<ICacheAdapter, CacheAdapter>();
    builder.Services.AddScoped<ISystemStudyFunctionService, SystemStudyFunctionService>();
    builder.Services.AddSingleton<IGlobalDataService, GlobalDataService>();
    builder.Services.AddScoped<IRoleAuthorizationService, RoleAuthorizationService>();
    builder.Services.AddScoped<IAuthorizationHandler, HardRoleHandler>();

    builder.Services.AddScoped<IDbConnection>(sp => 
        new SqlConnection(sp.GetRequiredService<IConfiguration>()
            .GetConnectionString("DefaultConnection")));


    builder.Services.AddAuthorizationCore(options =>
    {
        options.AddPolicy("RequireSuperAdmin", policy =>
            policy.RequireRole(HardRoles.SuperAdmin));
    
        options.AddPolicy("RequireAdmin", policy =>
            policy.RequireRole(HardRoles.Admin, HardRoles.SuperAdmin));
    
        options.AddPolicy("RequireUser", policy =>
            policy.RequireRole(HardRoles.User, HardRoles.Admin, HardRoles.SuperAdmin));
    
        options.AddPolicy("RequireReport", policy =>
            policy.RequireRole(HardRoles.Report, HardRoles.User, HardRoles.Admin, HardRoles.SuperAdmin));
    
        options.AddPolicy("RequireGuest", policy =>
            policy.RequireRole(HardRoles.Guest, HardRoles.Report, HardRoles.User, HardRoles.Admin, HardRoles.SuperAdmin));
    });

    builder.Services.AddScoped<IAuthorizationHandler, HardRoleHandler>();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseAuthentication();
app.UseAuthorization();


if (app.Environment.IsDevelopment())
{
    // comment is auto logoin as seed (admin) is not required
    //app.UseMiddleware<DevAutoLoginMiddleware>();
}


app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        await SeedData.InitializeAsync(userManager, roleManager);
        Console.WriteLine("Admin user seeding completed.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}





app.Run();


public class ConnectionStrings
{
    public string? DefaultConnection { get; set; }
    public string? MacMini { get; set; }
    public string? MacBook { get; set; }
    public string? OracleVM1VCU { get; set; }
    public string? TestFeb24Context { get; set; }
    public string? MacMiniDocker { get; set; }
    public string? Redis { get; set; }
}