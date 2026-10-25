
// App #1 - Atlas TaskFlow
// Program.cs - Register application database services

using Atlas_TaskFlow_App.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


// ========================================
// 1. Atlas TaskFlow Database
// ========================================

builder.Services.AddDbContext<TaskFlowDbContext>(
    options => options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "TaskFlowConnection"
        )
    )
);


// ========================================
// 2. ASP.NET Core Identity Database
// ========================================

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found."
    );

builder.Services.AddDbContext<ApplicationDbContext>(
    options => options.UseSqlServer(connectionString)
);

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(
    options => options.SignIn.RequireConfirmedAccount = true
)
.AddEntityFrameworkStores<ApplicationDbContext>();


// ========================================
// 3. MVC Services
// ========================================

builder.Services.AddControllersWithViews();


// ========================================
// 4. Team Database Context
// ========================================

builder.Services.AddDbContext<TeamDataContext>(
    options => options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "TaskFlowConnection"
        )
    )
);


// ========================================
// 5. Notifications Database Context
// NEW - Fixes the Dependency Injection error
// ========================================

builder.Services.AddDbContext<NotificationDataContext>(
    options => options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "TaskFlowConnection"
        )
    )
);


// ========================================
// 6. Build the Application
// ========================================

var app = builder.Build();


// ========================================
// 7. Configure HTTP Pipeline
// ========================================

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

app.MapStaticAssets();


// ========================================
// 8. MVC Routing
// ========================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
)
.WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();
