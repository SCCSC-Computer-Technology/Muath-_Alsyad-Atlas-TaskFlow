
// App #1 - Atlas TaskFlow
// Dashboard Controller - Current User Data

using System;
using System.Linq;
using System.Security.Claims;
using Atlas_TaskFlow_App.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atlas_TaskFlow_App.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly TaskFlowDbContext _db;

        public DashboardController(TaskFlowDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            // Get the logged-in user's Identity ID
            string? userId = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            // Only tasks from projects owned by this user
            var tasks = _db.Tasks
                .Include(t => t.Project)
                .Where(t =>
                    t.Project != null &&
                    t.Project.OwnerUserId == userId)
                .OrderBy(t => t.DueDate)
                .ToList();

            // Only this user's active projects
            ViewBag.ActiveProjects = _db.Projects
                .Count(p =>
                    p.OwnerUserId == userId &&
                    p.Status == "Active");

            // Only this user's projects in Create Task
            ViewBag.ProjectList = _db.Projects
                .Where(p => p.OwnerUserId == userId)
                .OrderBy(p => p.Name)
                .ToList();

            return View(tasks);
        }
    }
}
