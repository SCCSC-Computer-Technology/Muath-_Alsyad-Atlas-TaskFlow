
// App #1 - Atlas TaskFlow - Tasks Controller

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Atlas_TaskFlow_App.Data;
using Atlas_TaskFlow_App.Models;
using System.Text;

namespace Atlas_TaskFlow_App.Controllers
{
    public class TasksController : Controller
    {
        private readonly TaskFlowDbContext _db;

        public TasksController(TaskFlowDbContext db)
        {
            _db = db;
        }

        // ======================================
        // 1. MY TASKS PAGE - GET
        // ======================================

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

            DateTime nextMonday =
                monday.AddDays(7);

            // Statistics for the summary cards
            ViewBag.TotalTasks = _db.Tasks.Count();

            ViewBag.CompletedTasks = _db.Tasks
                .Count(t => t.Status == "Completed");

            ViewBag.OverdueTasks = _db.Tasks
                .Count(t =>
                    t.DueDate < today &&
                    t.Status != "Completed");

            ViewBag.DueThisWeek = _db.Tasks
                .Count(t =>
                    t.DueDate >= monday &&
                    t.DueDate < nextMonday &&
                    t.Status != "Completed");

            // Tasks with upcoming deadlines
            ViewBag.UpcomingTasks = _db.Tasks
                .Where(t =>
                    t.DueDate >= today &&
                    t.Status != "Completed")
                .OrderBy(t => t.DueDate)
                .Take(5)
                .ToList();

            // Start building the main tasks query
            var query = _db.Tasks
                .Include(t => t.Project)
                .AsQueryable();

            // Search tasks and project names
            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim();

                query = query.Where(t =>
                    t.Title.Contains(term) ||
                    (t.Project != null &&
                     t.Project.Name.Contains(term)));
            }

            // Filter by project
            if (projectId.HasValue && projectId.Value > 0)
            {
                query = query.Where(t =>
                    t.ProjectId == projectId.Value);
            }

            // Filter by priority
            if (!string.IsNullOrEmpty(priority))
            {
                query = query.Where(t =>
                    t.Priority == priority);
            }

            // Filter by status
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(t =>
                    t.Status == status);
            }

            // Quick date/status filters
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

            // Projects for filters and Create Task Modal
            ViewBag.ProjectList = _db.Projects
                .OrderBy(p => p.Name)
                .ToList();

            // Keep selected filter values
            ViewBag.Search = search ?? "";
            ViewBag.ProjectId = projectId ?? 0;
            ViewBag.Priority = priority ?? "";
            ViewBag.Status = status ?? "";
            ViewBag.Period = period ?? "";
            ViewBag.Sort = sort ?? "";

            // Execute the query and show the View
            return View(query.ToList());
        }

        // ======================================
        // 2. CREATE TASK - GET
        // ======================================

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // ======================================
        // 3. CREATE TASK - POST
        // ======================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(
            [Bind("Title,Description,DueDate,Priority,Status,ProjectId")]
            TaskItem task)
        {
            if (!ModelState.IsValid || !ValidTask(task))
            {
                TempData["CreateTaskError"] =
                    "Please check your task information.";

                return RedirectToAction("Index", "Dashboard");
            }

            task.Title = task.Title.Trim();

            _db.Tasks.Add(task);
            _db.SaveChanges();

            TempData["TaskMessage"] =
                "Task created successfully!";

            return RedirectToAction(nameof(Index));
        }

        // ======================================
        // 4. EDIT TASK - GET
        // ======================================

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var task = _db.Tasks.Find(id);

            if (task == null)
            {
                return NotFound();
            }

            LoadProjects();

            return View(task);
        }

        // ======================================
        // 5. EDIT TASK - POST
        // ======================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(
            int id,
            [Bind("TaskId,Title,Description,DueDate,Priority,Status,ProjectId")]
            TaskItem input)
        {
            if (id != input.TaskId)
            {
                return BadRequest();
            }

            var task = _db.Tasks.Find(id);

            if (task == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid || !ValidTask(input))
            {
                LoadProjects();

                ViewBag.Error =
                    "Please check the task information.";

                return View(input);
            }

            // Update the existing task
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

        // ======================================
        // 6. DELETE TASK - POST
        // ======================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var task = _db.Tasks.Find(id);

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

        // ======================================
        // 7. EXPORT TASKS TO CSV
        // ======================================

        [HttpGet]
        public IActionResult Export()
        {
            var tasks = _db.Tasks
                .Include(t => t.Project)
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

        // ======================================
        // HELPER: VALIDATE TASK
        // ======================================

        private bool ValidTask(TaskItem task)
        {
            return
                !string.IsNullOrWhiteSpace(task.Title) &&
                task.Title.Length <= 200 &&
                (task.Description == null ||
                 task.Description.Length <= 1000) &&
                task.DueDate.HasValue &&
                new[] { "Low", "Medium", "High", "Urgent" }
                    .Contains(task.Priority) &&
                new[] { "To Do", "In Progress", "Completed" }
                    .Contains(task.Status) &&
                _db.Projects.Any(p =>
                    p.ProjectId == task.ProjectId);
        }

        // HELPER: LOAD PROJECTS
        private void LoadProjects()
        {
            ViewBag.ProjectList = _db.Projects
                .OrderBy(p => p.Name)
                .ToList();
        }

        // HELPER: SAFE CSV VALUE
        private static string CsvValue(string value)
        {
            if (value.Length > 0 &&
                "=+-@\t\r".Contains(value[0]))
            {
                value = "'" + value;
            }

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
