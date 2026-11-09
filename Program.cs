
// App #1 - Atlas TaskFlow
// Program.cs - Identity + Protected MVC Pages
// ASP.NET Core MVC + SQL Server - Database First

using Atlas_TaskFlow_App.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. TASKS AND PROJECTS DATABASE
// ==========================================

builder.Services.AddDbContext<TaskFlowDbContext>(
    options => options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "TaskFlowConnection")
    )
);

// ==========================================
// 2. IDENTITY DATABASE
// ==========================================

var connectionString =
    builder.Configuration.GetConnectionString(
        "DefaultConnection")
    ?? throw new InvalidOperationException(
        "DefaultConnection was not found."
    );

builder.Services.AddDbContext<ApplicationDbContext>(
    options => options.UseSqlServer(connectionString)
);

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Use the original ASP.NET Core Identity.
builder.Services
    .AddDefaultIdentity<IdentityUser>(
        options =>
            options.SignIn.RequireConfirmedAccount = true
    )
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Send unauthenticated users to our Atlas Login page.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/";
});

// ==========================================
// 3. MVC AND RAZOR PAGES
// ==========================================

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// ==========================================
// 4. PROTECT APPLICATION PAGES
// ==========================================

// Every page requires login unless it has
// [AllowAnonymous] or AllowAnonymous().
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy =
        new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
});

// ==========================================
// 5. TEAM DATABASE
// ==========================================

builder.Services.AddDbContext<TeamDataContext>(
    options => options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "TaskFlowConnection")
    )
);

// ==========================================
// 6. NOTIFICATIONS DATABASE
// ==========================================

builder.Services.AddDbContext<NotificationDataContext>(
    options => options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "TaskFlowConnection")
    )
);

// ==========================================
// 7. USER SETTINGS DATABASE
// ==========================================

builder.Services.AddDbContext<UserSettingsDataContext>(
    options => options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "TaskFlowConnection")
    )
);

// ==========================================
// 8. BUILD THE APPLICATION
// ==========================================

var app = builder.Build();

// ==========================================
// 9. REQUEST PIPELINE
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

// Check who the user is.
app.UseAuthentication();

// Check whether the user is allowed.
app.UseAuthorization();

// CSS, JS and images must remain public.
app.MapStaticAssets()
   .AllowAnonymous();

// ==========================================
// 10. MVC ROUTING
// ==========================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
)
.WithStaticAssets();

// ==========================================
// 11. IDENTITY PAGES
// ==========================================

app.MapRazorPages()
   .WithStaticAssets();

// ==========================================
// 12. START
// ==========================================

app.Run();
