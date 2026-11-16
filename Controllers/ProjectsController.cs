
// App #1 - Atlas TaskFlow
// Projects Controller - Admin-owned Projects
// ASP.NET Core MVC + SQL Server - Database First

using System;
using System.Linq;
using Atlas_TaskFlow_App.Data;
using Atlas_TaskFlow_App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atlas_TaskFlow_App.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProjectsController : Controller
    {
        private readonly TaskFlowDbContext _db;

        public ProjectsController(TaskFlowDbContext db)
        {
            _db = db;
        }

        private string CurrentUserId()
        {
            return TaskAccess.CurrentUserId(User);
        }

        // =====================================
        // 1. MY PROJECTS
        // =====================================
        [HttpGet]
        public IActionResult Index(
            string? search,
            string? status)
        {
            string userId = CurrentUserId();

            var query = _db.Projects
                .AsNoTracking()
                .Include(p => p.Tasks)
                .Where(p => p.OwnerUserId == userId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim();

                query = query.Where(p =>
                    p.Name.Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(p =>
                    p.Status == status);
            }

            ViewBag.TotalProjects =
                _db.Projects.Count(p =>
                    p.OwnerUserId == userId);

            ViewBag.ActiveProjects =
                _db.Projects.Count(p =>
                    p.OwnerUserId == userId &&
                    p.Status == "Active");

            ViewBag.TotalTasks =
                _db.Tasks.Count(t =>
                    t.Project != null &&
                    t.Project.OwnerUserId == userId);

            ViewBag.CompletedTasks =
                _db.Tasks.Count(t =>
                    t.Project != null &&
                    t.Project.OwnerUserId == userId &&
                    t.Status == "Completed");

            ViewBag.Search = search ?? "";
            ViewBag.Status = status ?? "";

            var projects = query
                .OrderBy(p => p.Name)
                .ToList();

            ViewBag.ProjectList = _db.Projects
                .Where(p => p.OwnerUserId == userId)
                .OrderBy(p => p.Name)
                .ToList();

            return View(projects);
        }

        // =====================================
        // 2. CREATE PROJECT
        // =====================================
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Project
            {
                Status = "Active",
                OwnerUserId = CurrentUserId()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(
            [Bind("Name,Description,StartDate,DueDate,Status")]
            Project input)
        {
            string userId = CurrentUserId();

            // Owner comes from Identity.
            input.OwnerUserId = userId;

            ModelState.Remove(
                nameof(Project.OwnerUserId));

            if (!ModelState.IsValid ||
                !ValidProject(input))
            {
                ViewBag.Error =
                    "Please check the project information.";

                return View(input);
            }

            var project = new Project
            {
                Name = input.Name.Trim(),
                Description = input.Description?.Trim(),
                StartDate = input.StartDate,
                DueDate = input.DueDate,
                Status = input.Status,
                OwnerUserId = userId
            };

            _db.Projects.Add(project);
            _db.SaveChanges();

            TempData["ProjectMessage"] =
                "Project created successfully!";

            return RedirectToAction(nameof(Index));
        }

        // =====================================
        // 3. EDIT PROJECT
        // =====================================
        [HttpGet]
        public IActionResult Edit(int id)
        {
            string userId = CurrentUserId();

            var project = _db.Projects
                .FirstOrDefault(p =>
                    p.ProjectId == id &&
                    p.OwnerUserId == userId);

            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(
            int id,
            [Bind("ProjectId,Name,Description,StartDate,DueDate,Status")]
            Project input)
        {
            string userId = CurrentUserId();

            if (id != input.ProjectId)
            {
                return BadRequest();
            }

            var project = _db.Projects
                .FirstOrDefault(p =>
                    p.ProjectId == id &&
                    p.OwnerUserId == userId);

            if (project == null)
            {
                return NotFound();
            }

            input.OwnerUserId = userId;

            ModelState.Remove(
                nameof(Project.OwnerUserId));

            if (!ModelState.IsValid ||
                !ValidProject(input))
            {
                ViewBag.Error =
                    "Please check the project information.";

                return View(input);
            }

            project.Name = input.Name.Trim();
            project.Description = input.Description?.Trim();
            project.StartDate = input.StartDate;
            project.DueDate = input.DueDate;
            project.Status = input.Status;

            // Do not change the owner.
            _db.SaveChanges();

            TempData["ProjectMessage"] =
                "Project updated successfully!";

            return RedirectToAction(nameof(Index));
        }

        // =====================================
        // 4. DELETE PROJECT
        // =====================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            string userId = CurrentUserId();

            var project = _db.Projects
                .FirstOrDefault(p =>
                    p.ProjectId == id &&
                    p.OwnerUserId == userId);

            if (project == null)
            {
                return NotFound();
            }

            bool hasTasks = _db.Tasks.Any(t =>
                t.ProjectId == id);

            if (hasTasks)
            {
                TempData["ProjectError"] =
                    "Cannot delete a project containing tasks.";

                return RedirectToAction(nameof(Index));
            }

            try
            {
                _db.Projects.Remove(project);
                _db.SaveChanges();

                TempData["ProjectMessage"] =
                    "Project deleted successfully!";
            }
            catch (DbUpdateException)
            {
                TempData["ProjectError"] =
                    "This project has related records and cannot be deleted.";
            }

            return RedirectToAction(nameof(Index));
        }

        // =====================================
        // 5. PROJECT VALIDATION
        // =====================================
        private bool ValidProject(Project project)
        {
            bool validName =
                !string.IsNullOrWhiteSpace(project.Name) &&
                project.Name.Trim().Length <= 150;

            bool validDescription =
                project.Description == null ||
                project.Description.Length <= 500;

            bool validDates =
                !project.StartDate.HasValue ||
                !project.DueDate.HasValue ||
                project.StartDate.Value <= project.DueDate.Value;

            bool validStatus =
                new[]
                {
                    "Planning",
                    "Active",
                    "On Hold",
                    "Completed"
                }.Contains(project.Status);

            return validName &&
                   validDescription &&
                   validDates &&
                   validStatus;
        }

        // =====================================
        // 6. DEVELOPMENT VIEW DIAGNOSTIC
        // =====================================
        [HttpGet]
        public IActionResult CheckView(
            [FromServices]
            Microsoft.AspNetCore.Hosting.IWebHostEnvironment env,
            [FromServices]
            Microsoft.AspNetCore.Mvc.ViewEngines.ICompositeViewEngine viewEngine)
        {
            if (!env.IsDevelopment())
            {
                return NotFound();
            }

            string filePath = System.IO.Path.Combine(
                env.ContentRootPath,
                "Views",
                "Projects",
                "Index.cshtml");

            bool fileExists =
                System.IO.File.Exists(filePath);

            var projectsView = viewEngine.GetView(
                null,
                "/Views/Projects/Index.cshtml",
                true);

            var tasksView = viewEngine.GetView(
                null,
                "/Views/Tasks/Index.cshtml",
                true);

            return Content(
                "Application Folder: " + env.ContentRootPath +
                "\n\nProjects File Exists: " + fileExists +
                "\nProjects Razor View Found: " + projectsView.Success +
                "\nTasks Razor View Found: " + tasksView.Success +
                "\n\nExpected File: " + filePath,
                "text/plain");
        }
    }
}
