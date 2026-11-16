
// App #1 - Atlas TaskFlow
// Tasks Controller - Real Admin / Member Authorization
// ASP.NET Core MVC + SQL Server - Database First

using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly TeamDataContext _teamDb;
        private readonly ApplicationDbContext _identityDb;

        public TasksController(
            TaskFlowDbContext db,
            TeamDataContext teamDb,
            ApplicationDbContext identityDb)
        {
            _db = db;
            _teamDb = teamDb;
            _identityDb = identityDb;
        }

        // Current authenticated account.
        private string CurrentUserId()
        {
            return TaskAccess.CurrentUserId(User);
        }

        // Use identical rules everywhere.
        private List<TaskItem> VisibleTasks()
        {
            return TaskAccess.GetVisibleTasks(
                _db,
                _teamDb,
                _identityDb,
                User);
        }

        // =====================================
        // 1. DISPLAY MY TASKS
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
            DateTime today = DateTime.Today;

            int daysSinceMonday =
                ((int)today.DayOfWeek + 6) % 7;

            DateTime monday =
                today.AddDays(-daysSinceMonday);

            DateTime nextMonday = monday.AddDays(7);

            // Shared Admin / Member task access.
            var allTasks = VisibleTasks();

            var projects = TaskAccess.GetVisibleProjects(
                _db,
                User,
                allTasks);

            ViewBag.ProjectList = projects;

            // Calculate statistics before filters.
            ViewBag.TotalTasks = allTasks.Count;

            ViewBag.CompletedTasks = allTasks.Count(t =>
                t.Status == "Completed");

            ViewBag.OverdueTasks = allTasks.Count(t =>
                t.DueDate.HasValue &&
                t.DueDate.Value.Date < today &&
                t.Status != "Completed");

            ViewBag.DueThisWeek = allTasks.Count(t =>
                t.DueDate >= today &&
                t.DueDate < nextMonday &&
                t.Status != "Completed");

            ViewBag.UpcomingTasks = allTasks
                .Where(t =>
                    t.DueDate >= today &&
                    t.Status != "Completed")
                .OrderBy(t => t.DueDate)
                .Take(5)
                .ToList();

            IEnumerable<TaskItem> query = allTasks;

            // Search titles or project names.
            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim();

                query = query.Where(t =>
                    t.Title.Contains(
                        term,
                        StringComparison.OrdinalIgnoreCase) ||
                    (t.Project?.Name.Contains(
                        term,
                        StringComparison.OrdinalIgnoreCase)
                        ?? false));
            }

            // Filter by project.
            if (projectId.HasValue &&
                projectId.Value > 0)
            {
                query = query.Where(t =>
                    t.ProjectId == projectId.Value);
            }

            // Filter by priority.
            if (!string.IsNullOrWhiteSpace(priority))
            {
                query = query.Where(t =>
                    t.Priority == priority);
            }

            // Filter by status.
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(t =>
                    t.Status == status);
            }

            // Date filters.
            if (period == "today")
            {
                query = query.Where(t =>
                    t.DueDate?.Date == today);
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

            // Sorting.
            if (sort == "newest")
            {
                query = query.OrderByDescending(t =>
                    t.TaskId);
            }
            else if (sort == "title")
            {
                query = query.OrderBy(t => t.Title);
            }
            else if (sort == "due-desc")
            {
                query = query.OrderByDescending(t =>
                    t.DueDate);
            }
            else
            {
                query = query
                    .OrderBy(t => t.DueDate)
                    .ThenBy(t => t.TaskId);
            }

            ViewBag.Search = search ?? "";
            ViewBag.ProjectId = projectId ?? 0;
            ViewBag.Priority = priority ?? "";
            ViewBag.Status = status ?? "";
            ViewBag.Period = period ?? "";
            ViewBag.Sort = sort ?? "";

            return View(query.ToList());
        }

        // =====================================
        // 2. CREATE TASK - ADMIN ONLY
        // =====================================
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            LoadProjects(CurrentUserId());

            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(
            [Bind("Title,Description,DueDate,Priority,Status,ProjectId")]
            TaskItem task)
        {
            string userId = CurrentUserId();

            if (!ModelState.IsValid ||
                !ValidTask(task, userId))
            {
                TempData["CreateTaskError"] =
                    "Please check your task information and project.";

                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }

            task.Title = task.Title.Trim();

            _db.Tasks.Add(task);
            _db.SaveChanges();

            TempData["TaskMessage"] =
                "Task created successfully!";

            return RedirectToAction(nameof(Index));
        }

        // =====================================
        // 3. EDIT TASK - ADMIN OWNER ONLY
        // =====================================
        [Authorize(Roles = "Admin")]
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

        [Authorize(Roles = "Admin")]
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

            // Verify ownership on the server.
            var task = _db.Tasks
                .FirstOrDefault(t =>
                    t.TaskId == id &&
                    t.Project != null &&
                    t.Project.OwnerUserId == userId);

            if (task == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid ||
                !ValidTask(input, userId))
            {
                LoadProjects(userId);

                ViewBag.Error =
                    "Please check the task information.";

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
        // 4. MEMBER UPDATES STATUS ONLY
        // =====================================
        [Authorize(Roles = "Member")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateStatus(
            int id,
            string status)
        {
            // Accept only the three allowed statuses.
            if (status != "To Do" &&
                status != "In Progress" &&
                status != "Completed")
            {
                return BadRequest();
            }

            // Verify the task belongs to this member.
            var assignedTask = VisibleTasks()
                .FirstOrDefault(t => t.TaskId == id);

            if (assignedTask == null)
            {
                return NotFound();
            }

            // Load the actual database row for editing.
            var task = _db.Tasks
                .FirstOrDefault(t =>
                    t.TaskId == id &&
                    t.ProjectId == assignedTask.ProjectId);

            if (task == null)
            {
                return NotFound();
            }

            // This is the ONLY field a Member can change.
            task.Status = status;

            _db.SaveChanges();

            TempData["TaskMessage"] =
                "Task status updated successfully!";

            return RedirectToAction(nameof(Index));
        }

        // =====================================
        // 5. DELETE TASK - ADMIN OWNER ONLY
        // =====================================
        [Authorize(Roles = "Admin")]
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

            try
            {
                _db.Tasks.Remove(task);
                _db.SaveChanges();

                TempData["TaskMessage"] =
                    "Task deleted successfully!";
            }
            catch (DbUpdateException)
            {
                // Assigned tasks or notifications may
                // prevent deletion because of foreign keys.
                TempData["TaskMessage"] =
                    "Task could not be deleted while related records exist. Remove its assignment first.";
            }

            return RedirectToAction(nameof(Index));
        }

        // =====================================
        // 6. EXPORT VISIBLE TASKS
        // =====================================
        [HttpGet]
        public IActionResult Export()
        {
            var tasks = VisibleTasks()
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
                    CsvValue(
                        task.DueDate?.ToString("yyyy-MM-dd") ?? ""),
                    CsvValue(task.Priority),
                    CsvValue(task.Status)
                }));
            }

            return File(
                new UTF8Encoding(true).GetBytes(
                    csv.ToString()),
                "text/csv",
                "AtlasTaskFlow_Tasks.csv");
        }

        // =====================================
        // 7. VALIDATE ADMIN TASK
        // =====================================
        private bool ValidTask(
            TaskItem task,
            string userId)
        {
            bool fieldsAreValid =
                !string.IsNullOrWhiteSpace(task.Title) &&
                task.Title.Length <= 200 &&
                (task.Description == null ||
                 task.Description.Length <= 1000) &&
                task.DueDate.HasValue &&
                new[] { "Low", "Medium", "High", "Urgent" }
                    .Contains(task.Priority) &&
                new[] { "To Do", "In Progress", "Completed" }
                    .Contains(task.Status);

            bool ownsProject = _db.Projects.Any(p =>
                p.ProjectId == task.ProjectId &&
                p.OwnerUserId == userId);

            return fieldsAreValid && ownsProject;
        }

        // =====================================
        // 8. LOAD ADMIN PROJECT OPTIONS
        // =====================================
        private void LoadProjects(string userId)
        {
            ViewBag.ProjectList = _db.Projects
                .Where(p => p.OwnerUserId == userId)
                .OrderBy(p => p.Name)
                .ToList();
        }

        // =====================================
        // 9. SAFE CSV FORMATTING
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
