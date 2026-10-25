
// App #1 - Atlas TaskFlow - Workspace Controller

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Atlas_TaskFlow_App.Data;
using System;
using System.Linq;

namespace Atlas_TaskFlow_App.Controllers
{
    public class WorkspaceController : Controller
    {
        private readonly TaskFlowDbContext _db;

        public WorkspaceController(TaskFlowDbContext db)
        {
            _db = db;
        }

        // TEAM TASKS - UI Preview
        [HttpGet]
        public IActionResult TeamTasks()
        {
            return View();
        }

        // CALENDAR - Real SQL Server Data
        [HttpGet]
        public IActionResult Calendar()
        {
            var tasks = _db.Tasks
                .Include(t => t.Project)
                .AsNoTracking()
                .Where(t => t.DueDate != null)
                .OrderBy(t => t.DueDate)
                .ToList();

            // Projects for the shared Create Task Modal
            ViewBag.ProjectList = _db.Projects
                .OrderBy(p => p.Name)
                .ToList();

            return View(tasks);
        }

        // NOTIFICATIONS - UI Preview
        [HttpGet]
        public IActionResult Notifications()
        {
            return View();
        }

        // SETTINGS - UI Preview
        [HttpGet]
        public IActionResult Settings()
        {
            return View();
        }

        // HELP - UI Preview
        [HttpGet]
        public IActionResult Help()
        {
            return View();
        }
    }
}
