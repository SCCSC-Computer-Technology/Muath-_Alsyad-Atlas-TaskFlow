
// App #1 - Atlas TaskFlow - Tasks Controller
// Step 6 - User-owned Tasks and Projects
// ASP.NET Core MVC + SQL Server - Database First

using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Atlas_TaskFlow_App.Data;
using Atlas_TaskFlow_App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atlas_TaskFlow_App.Controllers
{
    [Authorize]
    public class TasksController : Controller
    {
        private readonly TaskFlowDbContext _db;

        public TasksController(TaskFlowDbContext db)
        {
            _db = db;
        }

        // Current user's Identity ID
        private string CurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException(
                    "The current user ID is missing.");
        }

        // =====================================
        // 1. MY TASKS PAGE
        // =====================================
        [HttpGet]
        public IActionResult Index(
            string? search,
            int? projectId,
            string? priority,
            string? status,
            string? period,
            string? sort)
        {
            string userId = CurrentUserId();
            DateTime today = DateTime.Today;

            int daysSinceMonday =
                ((int)today.DayOfWeek + 6) % 7;

            DateTime monday = today.AddDays(-daysSinceMonday);
            DateTime nextMonday = monday.AddDays(7);

            // Start with only this user's tasks
            var myTasks = _db.Tasks
                .Where(t => t.Project != null &&
                            t.Project.OwnerUserId == userId);

            // Statistics
            ViewBag.TotalTasks = myTasks.Count();

            ViewBag.CompletedTasks = myTasks
                .Count(t => t.Status == "Completed");

            ViewBag.OverdueTasks = myTasks
                .Count(t => t.DueDate < today &&
                            t.Status != "Completed");

            ViewBag.DueThisWeek = myTasks
                .Count(t => t.DueDate >= monday &&
                            t.DueDate < nextMonday &&
                            t.Status != "Completed");

            ViewBag.UpcomingTasks = myTasks
                .Where(t => t.DueDate >= today &&
                            t.Status != "Completed")
                .OrderBy(t => t.DueDate)
                .Take(5)
                .ToList();

            var query = myTasks
                .Include(t => t.Project)
                .AsQueryable();

            // Search
            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim();

                query = query.Where(t =>
                    t.Title.Contains(term) ||
                    (t.Project != null &&
                     t.Project.Name.Contains(term)));
            }

            // Project filter
            if (projectId.HasValue && projectId.Value > 0)
            {
                query = query.Where(t =>
                    t.ProjectId == projectId.Value);
            }

            // Priority filter
            if (!string.IsNullOrEmpty(priority))
            {
                query = query.Where(t =>
                    t.Priority == priority);
            }

            // Status filter
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(t =>
                    t.Status == status);
            }

            // Date filters
            if (period == "today")
            {
                query = query.Where(t =>
                    t.DueDate == today);
            }
            else if (period == "week")
            {
                query = query.Where(t =>
                    t.DueDate >= monday &&
                    t.DueDate < nextMonday);
            }
            else if (period == "overdue")
            {
                query = query.Where(t =>
                    t.DueDate < today &&
                    t.Status != "Completed");
            }
            else if (period == "completed")
            {
                query = query.Where(t =>
                    t.Status == "Completed");
            }

            // Sorting
            if (sort == "newest")
            {
                query = query.OrderByDescending(t => t.TaskId);
            }
            else if (sort == "title")
            {
                query = query.OrderBy(t => t.Title);
            }
            else if (sort == "due-desc")
            {
                query = query.OrderByDescending(t => t.DueDate);
            }
            else
            {
                query = query
                    .OrderBy(t => t.DueDate)
                    .ThenBy(t => t.TaskId);
            }

            // Only my projects in the dropdown
            LoadProjects(userId);

            ViewBag.Search = search ?? "";
            ViewBag.ProjectId = projectId ?? 0;
            ViewBag.Priority = priority ?? "";
            ViewBag.Status = status ?? "";
            ViewBag.Period = period ?? "";
            ViewBag.Sort = sort ?? "";

            return View(query.ToList());
        }

        // =====================================
        // 2. CREATE TASK PAGE
        // =====================================
        [HttpGet]
        public IActionResult Create()
        {
            LoadProjects(CurrentUserId());
            return View();
        }

        // =====================================
        // 3. SAVE NEW TASK
        // =====================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(
            [Bind("Title,Description,DueDate,Priority,Status,ProjectId")]
            TaskItem task)
        {
            string userId = CurrentUserId();

            // ValidTask also checks project ownership.
            if (!ModelState.IsValid || !ValidTask(task, userId))
            {
                TempData["CreateTaskError"] =
                    "Please check your task information and project.";

                return RedirectToAction("Index", "Dashboard");
            }

            task.Title = task.Title.Trim();

            _db.Tasks.Add(task);
            _db.SaveChanges();

            TempData["TaskMessage"] =
                "Task created successfully!";

            return RedirectToAction(nameof(Index));
        }

        // =====================================
        // 4. EDIT TASK PAGE
        // =====================================
        [HttpGet]
        public IActionResult Edit(int id)
        {
            string userId = CurrentUserId();

            var task = _db.Tasks
                .FirstOrDefault(t =>
                    t.TaskId == id &&
                    t.Project != null &&
                    t.Project.OwnerUserId == userId);

            if (task == null)
            {
                return NotFound();
            }

            LoadProjects(userId);

            return View(task);
        }

        // =====================================
        // 5. SAVE TASK CHANGES
        // =====================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(
            int id,
            [Bind("TaskId,Title,Description,DueDate,Priority,Status,ProjectId")]
            TaskItem input)
        {
            string userId = CurrentUserId();

            if (id != input.TaskId)
            {
                return BadRequest();
            }

            // Existing task must belong to the user
            var task = _db.Tasks
                .FirstOrDefault(t =>
                    t.TaskId == id &&
                    t.Project != null &&
                    t.Project.OwnerUserId == userId);

            if (task == null)
            {
                return NotFound();
            }

            // New ProjectId must also belong to the user
            if (!ModelState.IsValid || !ValidTask(input, userId))
            {
                LoadProjects(userId);

                ViewBag.Error =
                    "Please check the task information and project.";

                return View(input);
            }

            task.Title = input.Title.Trim();
            task.Description = input.Description;
            task.DueDate = input.DueDate;
            task.Priority = input.Priority;
            task.Status = input.Status;
            task.ProjectId = input.ProjectId;

            _db.SaveChanges();

            TempData["TaskMessage"] =
                "Task updated successfully!";

            return RedirectToAction(nameof(Index));
        }

        // =====================================
        // 6. DELETE TASK
        // =====================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            string userId = CurrentUserId();

            var task = _db.Tasks
                .FirstOrDefault(t =>
                    t.TaskId == id &&
                    t.Project != null &&
                    t.Project.OwnerUserId == userId);

            if (task == null)
            {
                return NotFound();
            }

            _db.Tasks.Remove(task);
            _db.SaveChanges();

            TempData["TaskMessage"] =
                "Task deleted successfully!";

            return RedirectToAction(nameof(Index));
        }

        // =====================================
        // 7. EXPORT MY TASKS TO CSV
        // =====================================
        [HttpGet]
        public IActionResult Export()
        {
            string userId = CurrentUserId();

            var tasks = _db.Tasks
                .Include(t => t.Project)
                .Where(t => t.Project != null &&
                            t.Project.OwnerUserId == userId)
                .OrderBy(t => t.TaskId)
                .ToList();

            var csv = new StringBuilder();

            csv.AppendLine(
                "TaskId,Title,Project,DueDate,Priority,Status");

            foreach (var task in tasks)
            {
                csv.AppendLine(string.Join(",", new[]
                {
                    CsvValue(task.TaskId.ToString()),
                    CsvValue(task.Title),
                    CsvValue(task.Project?.Name ?? ""),
                    CsvValue(task.DueDate?.ToString("yyyy-MM-dd") ?? ""),
                    CsvValue(task.Priority),
                    CsvValue(task.Status)
                }));
            }

            return File(
                new UTF8Encoding(true).GetBytes(csv.ToString()),
                "text/csv",
                "AtlasTaskFlow_Tasks.csv");
        }

        // =====================================
        // 8. VALIDATE TASK AND PROJECT OWNER
        // =====================================
        private bool ValidTask(TaskItem task, string userId)
        {
            bool validFields =
                !string.IsNullOrWhiteSpace(task.Title) &&
                task.Title.Length <= 200 &&
                (task.Description == null ||
                 task.Description.Length <= 1000) &&
                task.DueDate.HasValue &&
                new[] { "Low", "Medium", "High", "Urgent" }
                    .Contains(task.Priority) &&
                new[] { "To Do", "In Progress", "Completed" }
                    .Contains(task.Status);

            // Do not trust ProjectId sent by the browser
            bool ownsProject = _db.Projects.Any(p =>
                p.ProjectId == task.ProjectId &&
                p.OwnerUserId == userId);

            return validFields && ownsProject;
        }

        // =====================================
        // 9. LOAD ONLY MY PROJECTS
        // =====================================
        private void LoadProjects(string userId)
        {
            ViewBag.ProjectList = _db.Projects
                .Where(p => p.OwnerUserId == userId)
                .OrderBy(p => p.Name)
                .ToList();
        }

        // =====================================
        // 10. SAFE CSV VALUE
        // =====================================
        private static string CsvValue(string value)
        {
            if (value.Length > 0 &&
                "=+-@\t\r".Contains(value[0]))
            {
                value = "'" + value;
            }

            return "\"" +
                   value.Replace("\"", "\"\"") +
                   "\"";
        }
    }
}
