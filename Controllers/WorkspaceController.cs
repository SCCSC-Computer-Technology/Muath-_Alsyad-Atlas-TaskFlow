
// App #1 - Atlas TaskFlow
// Workspace Controller - User-Owned Team and Calendar
// ASP.NET Core MVC + SQL Server - Database First

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using Atlas_TaskFlow_App.Data;
using Atlas_TaskFlow_App.Models;
using Atlas_TaskFlow_App.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atlas_TaskFlow_App.Controllers
{
    [Authorize]
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

        // Get logged-in Identity user ID
        private string CurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException(
                    "Current user ID is missing.");
        }

        // ==========================================
        // 1. TEAM TASKS - GET
        // ==========================================

        [HttpGet]
        public IActionResult TeamTasks(
            string? search,
            int? projectId,
            int? memberId,
            string? priority,
            string? status)
        {
            string userId = CurrentUserId();

            // Only my projects
            var projects = _db.Projects
                .AsNoTracking()
                .Where(p => p.OwnerUserId == userId)
                .OrderBy(p => p.Name)
                .ToList();

            // Only tasks from my projects
            var tasks = _db.Tasks
                .Include(t => t.Project)
                .AsNoTracking()
                .Where(t => t.Project != null &&
                            t.Project.OwnerUserId == userId)
                .OrderBy(t => t.DueDate)
                .ToList();

            // Only my team members
            var members = _team.TeamMembers
                .AsNoTracking()
                .Where(m => m.OwnerUserId == userId &&
                            m.IsActive)
                .OrderBy(m => m.FullName)
                .ToList();

            // Task IDs owned by this user
            var taskIds = tasks
                .Select(t => t.TaskId)
                .ToList();

            // Only assignments involving my tasks
            // and my own team members
            var assignments = _team.TaskAssignments
                .AsNoTracking()
                .Where(a =>
                    taskIds.Contains(a.TaskId) &&
                    a.Member.OwnerUserId == userId)
                .ToList();

            var assignmentsByTask = assignments
                .ToDictionary(a => a.TaskId, a => a.MemberId);

            var membersById = members
                .ToDictionary(m => m.MemberId);

            var allRows = new List<TeamTaskRowViewModel>();

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
                    ProjectName = task.Project?.Name
                                  ?? "Unknown Project",
                    DueDate = task.DueDate,
                    Priority = task.Priority,
                    Status = task.Status,
                    MemberId = assignedMemberId,
                    AssigneeName = assignedName
                });
            }

            // Search and filters
            var filtered = allRows.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim();

                filtered = filtered.Where(t =>
                    t.Title.Contains(
                        term,
                        StringComparison.OrdinalIgnoreCase) ||
                    t.ProjectName.Contains(
                        term,
                        StringComparison.OrdinalIgnoreCase) ||
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

            // Shared Create Task modal
            ViewBag.ProjectList = projects;

            return View(model);
        }

        // ==========================================
        // 2. CREATE TEAM MEMBER - POST
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateMember(
            string fullName,
            string? email,
            string role = "Member")
        {
            string userId = CurrentUserId();

            fullName = fullName?.Trim() ?? "";

            email = string.IsNullOrWhiteSpace(email)
                ? null
                : email.Trim().ToLowerInvariant();

            if (fullName.Length < 2 ||
                fullName.Length > 100)
            {
                TempData["TeamError"] =
                    "Member name must contain 2 to 100 characters.";

                return RedirectToAction(nameof(TeamTasks));
            }

            if (email != null &&
                (email.Length > 256 ||
                 !new EmailAddressAttribute().IsValid(email)))
            {
                TempData["TeamError"] =
                    "Please enter a valid email address.";

                return RedirectToAction(nameof(TeamTasks));
            }

            // Do not reveal another user's team records
            if (email != null &&
                _team.TeamMembers.Any(m =>
                    m.OwnerUserId == userId &&
                    m.Email == email))
            {
                TempData["TeamError"] =
                    "This email is already in your team.";

                return RedirectToAction(nameof(TeamTasks));
            }

            // Labels only, not Identity roles
            role = role == "Lead" ? "Lead" : "Member";

            var member = new TeamMember
            {
                FullName = fullName,
                Email = email,
                Role = role,
                IsActive = true,

                // The owner comes from the server
                OwnerUserId = userId
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
                    "Could not add the member. Check the details.";
            }

            return RedirectToAction(nameof(TeamTasks));
        }

        // ==========================================
        // 3. ASSIGN OR UNASSIGN TASK - POST
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AssignTask(
            int taskId,
            int memberId)
        {
            string userId = CurrentUserId();

            // Confirm this task belongs to my project
            bool ownsTask = _db.Tasks.Any(t =>
                t.TaskId == taskId &&
                t.Project != null &&
                t.Project.OwnerUserId == userId);

            if (!ownsTask)
            {
                return NotFound();
            }

            var existingAssignment = _team.TaskAssignments
                .SingleOrDefault(a => a.TaskId == taskId);

            // MemberId = 0 means unassign
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

            // The assigned member must belong to me too
            bool ownsMember = _team.TeamMembers.Any(m =>
                m.MemberId == memberId &&
                m.OwnerUserId == userId &&
                m.IsActive);

            if (!ownsMember)
            {
                return NotFound();
            }

            if (existingAssignment == null)
            {
                _team.TaskAssignments.Add(
                    new TaskAssignment
                    {
                        TaskId = taskId,
                        MemberId = memberId
                    });
            }
            else
            {
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
        // 4. CALENDAR - GET
        // ==========================================

        [HttpGet]
        public IActionResult Calendar()
        {
            string userId = CurrentUserId();

            // Only my scheduled tasks
            var tasks = _db.Tasks
                .Include(t => t.Project)
                .AsNoTracking()
                .Where(t =>
                    t.Project != null &&
                    t.Project.OwnerUserId == userId &&
                    t.DueDate != null)
                .OrderBy(t => t.DueDate)
                .ToList();

            // Only my projects in the modal and filters
            ViewBag.ProjectList = _db.Projects
                .Where(p => p.OwnerUserId == userId)
                .OrderBy(p => p.Name)
                .ToList();

            return View(tasks);
        }

        // ==========================================
        // 5. NOTIFICATIONS REDIRECT
        // ==========================================

        [HttpGet]
        public IActionResult Notifications()
        {
            return RedirectToAction(
                "Index",
                "Notifications");
        }

        // ==========================================
        // 6. SETTINGS REDIRECT
        // ==========================================

        [HttpGet]
        public IActionResult Settings()
        {
            return RedirectToAction(
                "Index",
                "Settings");
        }

        // ==========================================
        // 7. HELP PAGE
        // ==========================================

        [HttpGet]
        public IActionResult Help()
        {
            return View();
        }
    }
}
