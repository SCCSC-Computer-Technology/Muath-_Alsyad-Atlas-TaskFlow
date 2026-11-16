
// App #1 - Atlas TaskFlow
// Shared Task Access Rules
// Database First - Identity - Admin / Member

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Atlas_TaskFlow_App.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atlas_TaskFlow_App.Data
{
    public static class TaskAccess
    {
        // Get the current Identity user ID.
        public static string CurrentUserId(
            ClaimsPrincipal user)
        {
            return user.FindFirstValue(
                ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException(
                    "Current user ID is missing.");
        }

        // One shared rule for all pages.
        public static List<TaskItem> GetVisibleTasks(
            TaskFlowDbContext db,
            TeamDataContext teamDb,
            ApplicationDbContext identityDb,
            ClaimsPrincipal user)
        {
            string userId = CurrentUserId(user);

            // ADMIN:
            // An Admin sees tasks from projects they own.
            if (user.IsInRole("Admin"))
            {
                return db.Tasks
                    .AsNoTracking()
                    .Include(t => t.Project)
                    .Where(t =>
                        t.Project != null &&
                        t.Project.OwnerUserId == userId)
                    .OrderBy(t => t.DueDate)
                    .ThenBy(t => t.TaskId)
                    .ToList();
            }

            // Reject users without the Member role.
            if (!user.IsInRole("Member"))
            {
                return new List<TaskItem>();
            }

            // Get email from Identity, not an HTML field.
            // Only confirmed accounts can receive tasks.
            string? email = identityDb.Users
                .AsNoTracking()
                .Where(u =>
                    u.Id == userId &&
                    u.EmailConfirmed)
                .Select(u => u.Email)
                .SingleOrDefault();

            if (string.IsNullOrWhiteSpace(email))
            {
                return new List<TaskItem>();
            }

            string normalizedEmail =
                email.Trim().ToUpperInvariant();

            // Find assignments to this registered email.
            var assignments = teamDb.TaskAssignments
                .AsNoTracking()
                .Where(a =>
                    a.Member.IsActive &&
                    a.Member.Email != null &&
                    a.Member.Email.ToUpper() ==
                    normalizedEmail)
                .Select(a => new
                {
                    a.TaskId,
                    OwnerUserId = a.Member.OwnerUserId
                })
                .ToList();

            if (assignments.Count == 0)
            {
                return new List<TaskItem>();
            }

            // TaskId -> Team owner's Identity ID
            var assignmentOwners = assignments
                .ToDictionary(
                    a => a.TaskId,
                    a => a.OwnerUserId);

            int[] taskIds =
                assignmentOwners.Keys.ToArray();

            // Read tasks from the existing Tasks table.
            var candidates = db.Tasks
                .AsNoTracking()
                .Include(t => t.Project)
                .Where(t =>
                    taskIds.Contains(t.TaskId) &&
                    t.Project != null)
                .ToList();

            // SECURITY:
            // The project owner must also own
            // the team member who received the task.
            return candidates
                .Where(t =>
                    t.Project != null &&
                    assignmentOwners.TryGetValue(
                        t.TaskId,
                        out string? ownerId) &&
                    ownerId == t.Project.OwnerUserId)
                .OrderBy(t => t.DueDate)
                .ThenBy(t => t.TaskId)
                .ToList();
        }

        // Projects visible for display and filters.
        // Members see project names connected to their tasks.
        public static List<Project> GetVisibleProjects(
            TaskFlowDbContext db,
            ClaimsPrincipal user,
            List<TaskItem> visibleTasks)
        {
            if (user.IsInRole("Admin"))
            {
                string userId = CurrentUserId(user);

                return db.Projects
                    .AsNoTracking()
                    .Where(p => p.OwnerUserId == userId)
                    .OrderBy(p => p.Name)
                    .ToList();
            }

            if (user.IsInRole("Member"))
            {
                return visibleTasks
                    .Where(t => t.Project != null)
                    .Select(t => t.Project!)
                    .DistinctBy(p => p.ProjectId)
                    .OrderBy(p => p.Name)
                    .ToList();
            }

            return new List<Project>();
        }
    }
}
