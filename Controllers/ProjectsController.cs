
// App #1 - Atlas TaskFlow - Projects Controller

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Atlas_TaskFlow_App.Data;
using Atlas_TaskFlow_App.Models;
using System;
using System.Linq;

namespace Atlas_TaskFlow_App.Controllers
{
    public class ProjectsController : Controller
    {
        // Database Context
        private readonly TaskFlowDbContext _db;

        // Constructor
        public ProjectsController(TaskFlowDbContext db)
        {
            _db = db;
        }

        // =====================================
        // 1. SHOW ALL PROJECTS
        // GET: /Projects
        // =====================================

        [HttpGet]
        public IActionResult Index(string? search, string? status)
        {
            // Prepare the projects query
            var query = _db.Projects
                .Include(p => p.Tasks)
                .AsNoTracking()
                .AsQueryable();

            // Search by project name
            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim();

                query = query.Where(p =>
                    p.Name.Contains(term));
            }

            // Filter by project status
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(p =>
                    p.Status == status);
            }

            // Dashboard-style statistics
            ViewBag.TotalProjects = _db.Projects.Count();

            ViewBag.ActiveProjects = _db.Projects
                .Count(p => p.Status == "Active");

            ViewBag.TotalTasks = _db.Tasks.Count();

            ViewBag.CompletedTasks = _db.Tasks
                .Count(t => t.Status == "Completed");

            // Remember current filters
            ViewBag.Search = search ?? "";
            ViewBag.Status = status ?? "";

            // Retrieve the projects
            var projects = query
                .OrderBy(p => p.Name)
                .ToList();

            // Send the list to the View
            return View(projects);
        }

        // =====================================
        // 2. OPEN CREATE PROJECT PAGE
        // GET: /Projects/Create
        // =====================================

        [HttpGet]
        public IActionResult Create()
        {
            var project = new Project
            {
                Status = "Active"
            };

            return View(project);
        }

        // =====================================
        // 3. SAVE NEW PROJECT
        // POST: /Projects/Create
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(
            [Bind("Name,Description,StartDate,DueDate,Status")]
            Project input)
        {
            // Validate the submitted project
            if (!ModelState.IsValid || !ValidProject(input))
            {
                ViewBag.Error =
                    "Please check the project information.";

                return View(input);
            }

            // Create a new project object
            var project = new Project
            {
                Name = input.Name.Trim(),
                Description = input.Description?.Trim(),
                StartDate = input.StartDate,
                DueDate = input.DueDate,
                Status = input.Status
            };

            // Prepare project for database insertion
            _db.Projects.Add(project);

            // Save changes to SQL Server
            _db.SaveChanges();

            TempData["ProjectMessage"] =
                "Project created successfully!";

            return RedirectToAction(nameof(Index));
        }

        // =====================================
        // 4. OPEN EDIT PROJECT PAGE
        // GET: /Projects/Edit/3
        // =====================================

        [HttpGet]
        public IActionResult Edit(int id)
        {
            // Find the requested project
            var project = _db.Projects.Find(id);

            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // =====================================
        // 5. SAVE PROJECT CHANGES
        // POST: /Projects/Edit/3
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(
            int id,
            [Bind("ProjectId,Name,Description,StartDate,DueDate,Status")]
            Project input)
        {
            if (id != input.ProjectId)
            {
                return BadRequest();
            }

            // Find the existing project
            var project = _db.Projects.Find(id);

            if (project == null)
            {
                return NotFound();
            }

            // Validate submitted information
            if (!ModelState.IsValid || !ValidProject(input))
            {
                ViewBag.Error =
                    "Please check the project information.";

                return View(input);
            }

            // Update project properties
            project.Name = input.Name.Trim();
            project.Description = input.Description?.Trim();
            project.StartDate = input.StartDate;
            project.DueDate = input.DueDate;
            project.Status = input.Status;

            // Save updates to SQL Server
            _db.SaveChanges();

            TempData["ProjectMessage"] =
                "Project updated successfully!";

            return RedirectToAction(nameof(Index));
        }

        // =====================================
        // 6. DELETE PROJECT
        // POST: /Projects/Delete
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var project = _db.Projects.Find(id);

            if (project == null)
            {
                return NotFound();
            }

            // Protect projects containing tasks
            bool hasTasks = _db.Tasks.Any(t =>
                t.ProjectId == id);

            if (hasTasks)
            {
                TempData["ProjectError"] =
                    "Cannot delete a project that contains tasks.";

                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Mark project for deletion
                _db.Projects.Remove(project);

                // Execute deletion in SQL Server
                _db.SaveChanges();

                TempData["ProjectMessage"] =
                    "Project deleted successfully!";
            }
            catch (DbUpdateException)
            {
                TempData["ProjectError"] =
                    "Project could not be deleted because it is referenced by other records.";
            }

            return RedirectToAction(nameof(Index));
        }

        // =====================================
        // 7. VALIDATE PROJECT DATA
        // =====================================

        private bool ValidProject(Project project)
        {
            // Project name must not be empty
            bool validName =
                !string.IsNullOrWhiteSpace(project.Name) &&
                project.Name.Trim().Length <= 150;

            // Description must fit SQL column
            bool validDescription =
                project.Description == null ||
                project.Description.Length <= 500;

            // Due date cannot precede start date
            bool validDates =
                !project.StartDate.HasValue ||
                !project.DueDate.HasValue ||
                project.StartDate.Value <= project.DueDate.Value;

            // Allowed statuses
            bool validStatus =
                new[] { "Planning", "Active", "On Hold", "Completed" }
                    .Contains(project.Status);

            return validName &&
                   validDescription &&
                   validDates &&
                   validStatus;
        }


        // App #1 - Temporary Projects View Diagnostic

        [HttpGet]
        public IActionResult CheckView(
            [FromServices] Microsoft.AspNetCore.Hosting.IWebHostEnvironment env,
            [FromServices] Microsoft.AspNetCore.Mvc.ViewEngines.ICompositeViewEngine viewEngine)
        {
            string filePath = System.IO.Path.Combine(
                env.ContentRootPath,
                "Views",
                "Projects",
                "Index.cshtml"
            );

            bool fileExists = System.IO.File.Exists(filePath);

            var projectsView = viewEngine.GetView(
                null,
                "/Views/Projects/Index.cshtml",
                true
            );

            var tasksView = viewEngine.GetView(
                null,
                "/Views/Tasks/Index.cshtml",
                true
            );

            return Content(
                "Application Folder: " + env.ContentRootPath +
                "\n\nProjects File Exists: " + fileExists +
                "\nProjects Razor View Found: " + projectsView.Success +
                "\nTasks Razor View Found: " + tasksView.Success +
                "\n\nExpected File: " + filePath,
                "text/plain"
            );
        }

    }
}
