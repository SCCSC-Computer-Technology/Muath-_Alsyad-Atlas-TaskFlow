
// App #1 - Atlas TaskFlow - Workspace Controller

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Atlas_TaskFlow_App.Data;
using Atlas_TaskFlow_App.Models;
using Atlas_TaskFlow_App.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atlas_TaskFlow_App.Controllers
{
    public class WorkspaceController : Controller
    {
        private readonly TaskFlowDbContext _db;
        private readonly TeamDataContext _team;

        public WorkspaceController(
            TaskFlowDbContext db,
            TeamDataContext team)
        {
            _db = db;
            _team = team;
        }

        // ==========================================
        // TEAM TASKS - REAL DATABASE DATA
        // ==========================================

        [HttpGet]
        public IActionResult TeamTasks(
            string? search,
            int? projectId,
            int? memberId,
            string? priority,
            string? status)
        {
            // Read existing projects and tasks
            var projects = _db.Projects
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ToList();

            var tasks = _db.Tasks
                .Include(t => t.Project)
                .AsNoTracking()
                .OrderBy(t => t.DueDate)
                .ToList();

            // Read team records from SQL Server
            var members = _team.TeamMembers
                .AsNoTracking()
                .Where(m => m.IsActive)
                .OrderBy(m => m.FullName)
                .ToList();

            var assignments = _team.TaskAssignments
                .AsNoTracking()
                .ToList();

            // Make lookups for each task assignment
            var assignmentsByTask = assignments
                .ToDictionary(a => a.TaskId, a => a.MemberId);

            var membersById = members
                .ToDictionary(m => m.MemberId);

            var allRows = new System.Collections.Generic
                .List<TeamTaskRowViewModel>();

            foreach (var task in tasks)
            {
                int? assignedMemberId =
                    assignmentsByTask.TryGetValue(
                        task.TaskId,
                        out int foundMemberId)
                    ? foundMemberId
                    : null;

                string assignedName = "Unassigned";

                if (assignedMemberId.HasValue &&
                    membersById.TryGetValue(
                        assignedMemberId.Value,
                        out var assignedMember))
                {
                    assignedName = assignedMember.FullName;
                }

                allRows.Add(new TeamTaskRowViewModel
                {
                    TaskId = task.TaskId,
                    Title = task.Title,
                    ProjectId = task.ProjectId,

                    ProjectName =
                        task.Project?.Name ?? "Unknown Project",

                    DueDate = task.DueDate,
                    Priority = task.Priority,
                    Status = task.Status,

                    MemberId = assignedMemberId,
                    AssigneeName = assignedName
                });
            }

            // Filter in memory for this initial team MVP
            var filtered = allRows.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim();

                filtered = filtered.Where(t =>
                    t.Title.Contains(
                        term,
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    t.ProjectName.Contains(
                        term,
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    t.AssigneeName.Contains(
                        term,
                        StringComparison.OrdinalIgnoreCase));
            }

            if (projectId.HasValue)
            {
                filtered = filtered.Where(
                    t => t.ProjectId == projectId.Value);
            }

            if (memberId.HasValue)
            {
                // MemberId = 0 means unassigned
                filtered = memberId.Value == 0
                    ? filtered.Where(t => !t.MemberId.HasValue)
                    : filtered.Where(
                        t => t.MemberId == memberId.Value);
            }

            if (!string.IsNullOrWhiteSpace(priority))
            {
                filtered = filtered.Where(
                    t => t.Priority == priority);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                filtered = filtered.Where(
                    t => t.Status == status);
            }

            var model = new TeamTasksPageViewModel
            {
                AllTasks = allRows,
                Tasks = filtered.ToList(),
                Members = members,
                Projects = projects,

                Statuses = allRows
                    .Select(t => t.Status)
                    .Distinct()
                    .OrderBy(s => s)
                    .ToList(),

                Priorities = allRows
                    .Select(t => t.Priority)
                    .Distinct()
                    .OrderBy(p => p)
                    .ToList(),

                Search = search ?? "",
                ProjectId = projectId,
                MemberId = memberId,
                Priority = priority ?? "",
                Status = status ?? ""
            };

            // Used by the shared Create Task modal
            ViewBag.ProjectList = projects;

            return View(model);
        }

        // ==========================================
        // ADD TEAM MEMBER
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateMember(
            string fullName,
            string? email,
            string role = "Member")
        {
            fullName = fullName?.Trim() ?? "";
            email = string.IsNullOrWhiteSpace(email)
                ? null
                : email.Trim().ToLowerInvariant();

            // Validate name
            if (fullName.Length < 2 || fullName.Length > 100)
            {
                TempData["TeamError"] =
                    "Member name must contain 2 to 100 characters.";

                return RedirectToAction(nameof(TeamTasks));
            }

            // Validate optional email
            if (email != null &&
                (email.Length > 256 ||
                 !new EmailAddressAttribute().IsValid(email)))
            {
                TempData["TeamError"] =
                    "Please enter a valid email address.";

                return RedirectToAction(nameof(TeamTasks));
            }

            if (email != null &&
                _team.TeamMembers.Any(m => m.Email == email))
            {
                TempData["TeamError"] =
                    "A team member with this email already exists.";

                return RedirectToAction(nameof(TeamTasks));
            }

            // Roles are labels only, not access permissions
            role = role == "Lead" ? "Lead" : "Member";

            var member = new TeamMember
            {
                FullName = fullName,
                Email = email,
                Role = role,
                IsActive = true
            };

            try
            {
                _team.TeamMembers.Add(member);
                _team.SaveChanges();

                TempData["TeamMessage"] =
                    "Team member added successfully!";
            }
            catch (DbUpdateException)
            {
                TempData["TeamError"] =
                    "Could not add this member. Check the details.";
            }

            return RedirectToAction(nameof(TeamTasks));
        }

        // ==========================================
        // ASSIGN OR UNASSIGN A TASK
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AssignTask(
            int taskId,
            int memberId)
        {
            // Confirm task exists
            bool taskExists = _db.Tasks
                .Any(t => t.TaskId == taskId);

            if (!taskExists)
            {
                return NotFound();
            }

            var existingAssignment = _team.TaskAssignments
                .SingleOrDefault(a => a.TaskId == taskId);

            // MemberId = 0 removes the assignment
            if (memberId == 0)
            {
                if (existingAssignment != null)
                {
                    _team.TaskAssignments
                        .Remove(existingAssignment);

                    _team.SaveChanges();
                }

                TempData["TeamMessage"] =
                    "Task is now unassigned.";

                return RedirectToAction(nameof(TeamTasks));
            }

            // Confirm member exists and is active
            bool memberExists = _team.TeamMembers
                .Any(m =>
                    m.MemberId == memberId &&
                    m.IsActive);

            if (!memberExists)
            {
                TempData["TeamError"] =
                    "Please select a valid team member.";

                return RedirectToAction(nameof(TeamTasks));
            }

            if (existingAssignment == null)
            {
                // First assignment for this task
                _team.TaskAssignments.Add(
                    new TaskAssignment
                    {
                        TaskId = taskId,
                        MemberId = memberId
                    });
            }
            else
            {
                // Change the assigned member
                existingAssignment.MemberId = memberId;
            }

            try
            {
                _team.SaveChanges();

                TempData["TeamMessage"] =
                    "Task assignment saved successfully!";
            }
            catch (DbUpdateException)
            {
                TempData["TeamError"] =
                    "Could not save the assignment.";
            }

            return RedirectToAction(nameof(TeamTasks));
        }

        // ==========================================
        // CALENDAR - PREVIOUS WORKING FEATURE
        // ==========================================

        [HttpGet]
        public IActionResult Calendar()
        {
            var tasks = _db.Tasks
                .Include(t => t.Project)
                .AsNoTracking()
                .Where(t => t.DueDate != null)
                .OrderBy(t => t.DueDate)
                .ToList();

            ViewBag.ProjectList = _db.Projects
                .OrderBy(p => p.Name)
                .ToList();

            return View(tasks);
        }

        // ==========================================
        // REMAINING UI PREVIEW PAGES
        // ==========================================


        [HttpGet]
        public IActionResult Notifications()
        {
            return RedirectToAction("Index", "Notifications");
        }


        [HttpGet]
        public IActionResult Settings()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Help()
        {
            return View();
        }
    }
}
