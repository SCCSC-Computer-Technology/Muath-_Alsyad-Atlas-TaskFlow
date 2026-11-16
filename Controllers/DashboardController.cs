
// App #1 - Atlas TaskFlow
// Dashboard Controller - Shared Admin / Member Access

using System;
using System.Linq;
using Atlas_TaskFlow_App.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Atlas_TaskFlow_App.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly TaskFlowDbContext _db;
        private readonly TeamDataContext _teamDb;
        private readonly ApplicationDbContext _identityDb;

        public DashboardController(
            TaskFlowDbContext db,
            TeamDataContext teamDb,
            ApplicationDbContext identityDb)
        {
            _db = db;
            _teamDb = teamDb;
            _identityDb = identityDb;
        }

        [HttpGet]
        public IActionResult Index()
        {
            // Both Dashboard and My Tasks now use
            // the SAME permission-checking method.
            var tasks = TaskAccess.GetVisibleTasks(
                _db,
                _teamDb,
                _identityDb,
                User);

            var projects = TaskAccess.GetVisibleProjects(
                _db,
                User,
                tasks);

            // Statistics are based on visible data.
            ViewBag.ActiveProjects = projects.Count(
                p => p.Status == "Active");

            ViewBag.ProjectList = projects;

            return View(tasks);
        }
    }
}
