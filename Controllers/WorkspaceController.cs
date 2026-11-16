
// App #1 - Atlas TaskFlow
// WorkspaceController - Admin Team Management
// Member Calendar - Database First

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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
        private readonly ApplicationDbContext _identity;

        public WorkspaceController(
            TaskFlowDbContext db,
            TeamDataContext team,
            ApplicationDbContext identity)
        {
            _db = db;
            _team = team;
            _identity = identity;
        }

        private string CurrentUserId()
        {
            return TaskAccess.CurrentUserId(User);
        }

        // ==========================================
        // 1. TEAM TASKS - ADMIN ONLY
        // ==========================================
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult TeamTasks(
            string? search,
            int? projectId,
            int? memberId,
            string? priority,
            string? status)
        {
            string userId = CurrentUserId();

            var projects = _db.Projects
                .AsNoTracking()
                .Where(p => p.OwnerUserId == userId)
                .OrderBy(p => p.Name)
                .ToList();

            var tasks = _db.Tasks
                .AsNoTracking()
                .Include(t => t.Project)
                .Where(t =>
                    t.Project != null &&
                    t.Project.OwnerUserId == userId)
                .OrderBy(t => t.DueDate)
                .ToList();

            var members = _team.TeamMembers
                .AsNoTracking()
                .Where(m =>
                    m.OwnerUserId == userId &&
                    m.IsActive)
                .OrderBy(m => m.FullName)
                .ToList();

            int[] taskIds = tasks
                .Select(t => t.TaskId)
                .ToArray();

            var assignments = _team.TaskAssignments
                .AsNoTracking()
                .Where(a =>
                    taskIds.Contains(a.TaskId) &&
                    a.Member.OwnerUserId == userId)
                .ToList();

            var assignmentMap = assignments
                .ToDictionary(
                    a => a.TaskId,
                    a => a.MemberId);

            var membersById = members
                .ToDictionary(m => m.MemberId);

            var allRows = new List<TeamTaskRowViewModel>();

            foreach (var task in tasks)
            {
                int? assignedMemberId =
                    assignmentMap.TryGetValue(
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

            IEnumerable<TeamTaskRowViewModel> filtered =
                allRows;

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
                filtered = filtered.Where(t =>
                    t.ProjectId == projectId.Value);
            }

            if (memberId.HasValue)
            {
                filtered = memberId.Value == 0
                    ? filtered.Where(t => !t.MemberId.HasValue)
                    : filtered.Where(t =>
                        t.MemberId == memberId.Value);
            }

            if (!string.IsNullOrWhiteSpace(priority))
            {
                filtered = filtered.Where(t =>
                    t.Priority == priority);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                filtered = filtered.Where(t =>
                    t.Status == status);
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

            ViewBag.ProjectList = projects;

            return View(model);
        }

        // ==========================================
        // 2. ADD MEMBER - ADMIN ONLY
        // ==========================================
        [Authorize(Roles = "Admin")]
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

            if (email == null ||
                email.Length > 256 ||
                !new EmailAddressAttribute().IsValid(email))
            {
                TempData["TeamError"] =
                    "Enter a valid registered member email.";

                return RedirectToAction(nameof(TeamTasks));
            }

            // A real account must exist and be confirmed.
            string normalizedEmail =
                email.ToUpperInvariant();

            bool registered = _identity.Users
                .AsNoTracking()
                .Any(u =>
                    u.NormalizedEmail == normalizedEmail &&
                    u.EmailConfirmed);

            if (!registered)
            {
                TempData["TeamError"] =
                    "This member must register and confirm their account first.";

                return RedirectToAction(nameof(TeamTasks));
            }

            bool alreadyExists = _team.TeamMembers.Any(m =>
                m.OwnerUserId == userId &&
                m.Email == email);

            if (alreadyExists)
            {
                TempData["TeamError"] =
                    "This email is already in your team.";

                return RedirectToAction(nameof(TeamTasks));
            }

            role = role == "Lead" ? "Lead" : "Member";

            var member = new TeamMember
            {
                FullName = fullName,
                Email = email,
                Role = role,
                IsActive = true,
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
                    "Could not add member. Check the email.";
            }

            return RedirectToAction(nameof(TeamTasks));
        }

        // ==========================================
        // 3. ASSIGN TASK - ADMIN ONLY
        // ==========================================
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AssignTask(
            int taskId,
            int memberId)
        {
            string userId = CurrentUserId();

            bool ownsTask = _db.Tasks.Any(t =>
                t.TaskId == taskId &&
                t.Project != null &&
                t.Project.OwnerUserId == userId);

            if (!ownsTask)
            {
                return NotFound();
            }

            var existingAssignment =
                _team.TaskAssignments
                    .SingleOrDefault(a =>
                        a.TaskId == taskId);

            // MemberId 0 means remove assignment.
            if (memberId == 0)
            {
                if (existingAssignment != null)
                {
                    _team.TaskAssignments.Remove(
                        existingAssignment);

                    _team.SaveChanges();
                }

                TempData["TeamMessage"] =
                    "Task is now unassigned.";

                return RedirectToAction(nameof(TeamTasks));
            }

            var member = _team.TeamMembers
                .AsNoTracking()
                .FirstOrDefault(m =>
                    m.MemberId == memberId &&
                    m.OwnerUserId == userId &&
                    m.IsActive);

            if (member == null ||
                string.IsNullOrWhiteSpace(member.Email))
            {
                return NotFound();
            }

            string normalizedEmail =
                member.Email.Trim().ToUpperInvariant();

            // Assignment must point to a real account.
            bool registered = _identity.Users
                .AsNoTracking()
                .Any(u =>
                    u.NormalizedEmail == normalizedEmail &&
                    u.EmailConfirmed);

            if (!registered)
            {
                TempData["TeamError"] =
                    "This team member needs a confirmed login account.";

                return RedirectToAction(nameof(TeamTasks));
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
        // 4. CALENDAR - ADMIN AND MEMBER
        // ==========================================
        [HttpGet]
        public IActionResult Calendar()
        {
            // Same access rules as Dashboard/My Tasks.
            var visibleTasks = TaskAccess.GetVisibleTasks(
                _db,
                _team,
                _identity,
                User);

            var projects = TaskAccess.GetVisibleProjects(
                _db,
                User,
                visibleTasks);

            var scheduledTasks = visibleTasks
                .Where(t => t.DueDate.HasValue)
                .OrderBy(t => t.DueDate)
                .ToList();

            ViewBag.ProjectList = projects;

            return View(scheduledTasks);
        }

        // ==========================================
        // 5. EXISTING NAVIGATION ACTIONS
        // ==========================================
        [HttpGet]
        public IActionResult Notifications()
        {
            return RedirectToAction(
                "Index",
                "Notifications");
        }

        [HttpGet]
        public IActionResult Settings()
        {
            return RedirectToAction(
                "Index",
                "Settings");
        }

        [HttpGet]
        public IActionResult Help()
        {
            return View();
        }
    }
}
