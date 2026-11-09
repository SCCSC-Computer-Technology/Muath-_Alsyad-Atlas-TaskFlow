
// App #1 - Atlas TaskFlow
// Program.cs - Identity, Roles, MVC and SQL Server
// Database First

using System;
using Atlas_TaskFlow_App.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. DATABASE CONNECTIONS
// ==========================================

// Tasks and Projects
builder.Services.AddDbContext<TaskFlowDbContext>(
    options => options.UseSqlServer(
        builder.Configuration.GetConnectionString("TaskFlowConnection")
    )
);

// Identity Accounts
string identityConnection =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "DefaultConnection was not found.");

builder.Services.AddDbContext<ApplicationDbContext>(
    options => options.UseSqlServer(identityConnection)
);

// Team Members and Assignments
builder.Services.AddDbContext<TeamDataContext>(
    options => options.UseSqlServer(
        builder.Configuration.GetConnectionString("TaskFlowConnection")
    )
);

// Notifications
builder.Services.AddDbContext<NotificationDataContext>(
    options => options.UseSqlServer(
        builder.Configuration.GetConnectionString("TaskFlowConnection")
    )
);

// User Settings
builder.Services.AddDbContext<UserSettingsDataContext>(
    options => options.UseSqlServer(
        builder.Configuration.GetConnectionString("TaskFlowConnection")
    )
);

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ==========================================
// 2. ASP.NET CORE IDENTITY + ROLES
// ==========================================

builder.Services
    .AddDefaultIdentity<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Keep our beautiful Atlas Login page
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/";
    options.AccessDeniedPath = "/Home/AccessDenied";
});

// ==========================================
// 3. MVC AND RAZOR PAGES
// ==========================================

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// ==========================================
// 4. REQUIRE LOGIN FOR APP PAGES
// ==========================================

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy =
        new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
});

// ==========================================
// 5. BUILD APP
// ==========================================

var app = builder.Build();

// ==========================================
// 6. CREATE ADMIN AND MEMBER ROLES
// ==========================================

// This creates role definitions, not admin users.
// A public registration never becomes Admin here.

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    string[] roles = { "Admin", "Member" };

    foreach (string roleName in roles)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var result = await roleManager.CreateAsync(
                new IdentityRole(roleName)
            );

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    "Could not create role: " + roleName
                );
            }
        }
    }
}

// ==========================================
// 7. HTTP PIPELINE
// ==========================================

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

// Allow CSS, JS and images without login
app.MapStaticAssets().AllowAnonymous();

// ==========================================
// 8. MVC ROUTING
// ==========================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
)
.WithStaticAssets();

// ==========================================
// 9. IDENTITY RAZOR PAGES
// ==========================================

app.MapRazorPages().WithStaticAssets();

// ==========================================
// 10. RUN
// ==========================================

app.Run();
