
// App #1 - Atlas TaskFlow
// Program.cs - Complete Application Configuration
// ASP.NET Core MVC + SQL Server + Identity
// Database First

using Atlas_TaskFlow_App.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


// =====================================================
// 1. MAIN ATLAS TASKFLOW DATABASE
// =====================================================
// Used for Projects and Tasks.
// Connection: TaskFlowConnection

builder.Services.AddDbContext<TaskFlowDbContext>(
    options => options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "TaskFlowConnection"
        )
    )
);


// =====================================================
// 2. ASP.NET CORE IDENTITY DATABASE
// =====================================================
// Used for Login, Register, and account management.
// Connection: DefaultConnection

var connectionString =
    builder.Configuration.GetConnectionString(
        "DefaultConnection"
    )
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found."
    );

builder.Services.AddDbContext<ApplicationDbContext>(
    options => options.UseSqlServer(connectionString)
);

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services
    .AddDefaultIdentity<IdentityUser>(
        options =>
            options.SignIn.RequireConfirmedAccount = true
    )
    .AddEntityFrameworkStores<ApplicationDbContext>();


// =====================================================
// 3. ASP.NET CORE MVC AND RAZOR PAGES
// =====================================================

builder.Services.AddControllersWithViews();

// Required for Razor Pages, including Identity UI.
builder.Services.AddRazorPages();


// =====================================================
// 4. TEAM DATABASE
// =====================================================
// Used for TeamMembers and TaskAssignments.
// Connection: TaskFlowConnection

builder.Services.AddDbContext<TeamDataContext>(
    options => options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "TaskFlowConnection"
        )
    )
);


// =====================================================
// 5. NOTIFICATIONS DATABASE
// =====================================================
// Used for saving notifications and read status.
// Connection: TaskFlowConnection

builder.Services.AddDbContext<NotificationDataContext>(
    options => options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "TaskFlowConnection"
        )
    )
);


// =====================================================
// 6. USER SETTINGS DATABASE
// =====================================================
// Used for user profiles and preferences.
// Connection: TaskFlowConnection

builder.Services.AddDbContext<UserSettingsDataContext>(
    options => options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "TaskFlowConnection"
        )
    )
);


// =====================================================
// 7. BUILD APPLICATION
// =====================================================

var app = builder.Build();


// =====================================================
// 8. HTTP REQUEST PIPELINE
// =====================================================

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");

    app.UseHsts();
}


// Redirect HTTP to HTTPS.
app.UseHttpsRedirection();


// Enable routing.
app.UseRouting();


// Identify the logged-in user.
app.UseAuthentication();


// Check user authorization permissions.
app.UseAuthorization();


// Enable application static assets.
app.MapStaticAssets();


// =====================================================
// 9. MVC ROUTING
// =====================================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
)
.WithStaticAssets();


// =====================================================
// 10. IDENTITY RAZOR PAGES
// =====================================================

app.MapRazorPages()
   .WithStaticAssets();


// =====================================================
// 11. START APPLICATION
// =====================================================

app.Run();
