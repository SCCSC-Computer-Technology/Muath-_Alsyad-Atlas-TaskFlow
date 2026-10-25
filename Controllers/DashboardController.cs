
// App #1 - Atlas TaskFlow - Dashboard Controller

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Atlas_TaskFlow_App.Data;

namespace Atlas_TaskFlow_App.Controllers
{
    public class DashboardController : Controller
    {
        private readonly TaskFlowDbContext _db;

        public DashboardController(TaskFlowDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            // Get all tasks with their projects
            var tasks = _db.Tasks
                .Include(t => t.Project)
                .OrderBy(t => t.DueDate)
                .ToList();

            // Count active projects
            ViewBag.ActiveProjects = _db.Projects
                .Count(p => p.Status == "Active");

            // Get projects for Create Task dropdown
            ViewBag.ProjectList = _db.Projects
                .OrderBy(p => p.Name)
                .ToList();

            return View(tasks);
        }
    }
}
