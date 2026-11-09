
// App #1 - Atlas TaskFlow
// Notifications Controller - User-Owned Notifications
// ASP.NET Core MVC + SQL Server - Database First

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Atlas_TaskFlow_App.Data;
using Atlas_TaskFlow_App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atlas_TaskFlow_App.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly TaskFlowDbContext _taskDb;
        private readonly TeamDataContext _teamDb;
        private readonly NotificationDataContext _notificationDb;

        public NotificationsController(
            TaskFlowDbContext taskDb,
            TeamDataContext teamDb,
            NotificationDataContext notificationDb)
        {
            _taskDb = taskDb;
            _teamDb = teamDb;
            _notificationDb = notificationDb;
        }

        // =========================================
        // CURRENT LOGGED-IN USER
        // =========================================

        private string CurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException(
                    "Current user ID is missing.");
        }

        // =========================================
        // FIND MY TASK IDs
        // =========================================

        private int[] MyTaskIds(string userId)
        {
            return _taskDb.Tasks
                .AsNoTracking()
                .Where(t => t.Project != null &&
                            t.Project.OwnerUserId == userId)
                .Select(t => t.TaskId)
                .ToArray();
        }

        // =========================================
        // 1. SHOW MY NOTIFICATIONS
        // =========================================

        [HttpGet]
        public IActionResult Index(string? filter)
        {
            string userId = CurrentUserId();
            int[] myTaskIds = MyTaskIds(userId);

            // Create notifications for this user's tasks
            SyncNotifications(userId, myTaskIds);

            filter = NormalizeFilter(filter);

            // Only notifications linked to my tasks
            var myNotifications = _notificationDb.Notifications
                .AsNoTracking()
                .Where(n =>
                    n.TaskId.HasValue &&
                    myTaskIds.Contains(n.TaskId.Value));

            // User-specific statistics
            ViewBag.TotalCount = myNotifications.Count();

            ViewBag.UnreadCount = myNotifications
                .Count(n => !n.IsRead);

            ViewBag.DeadlineCount = myNotifications
                .Count(n => n.Type == "deadline");

            ViewBag.AssignmentCount = myNotifications
                .Count(n => n.Type == "assignment");

            ViewBag.CompletedCount = myNotifications
                .Count(n => n.Type == "completed");

            ViewBag.Filter = filter;

            // Filter only the user's notifications
            var query = myNotifications;

            if (filter == "unread")
            {
                query = query.Where(n => !n.IsRead);
            }
            else if (filter != "all")
            {
                query = query.Where(n => n.Type == filter);
            }

            var notifications = query
                .OrderByDescending(n => n.CreatedAt)
                .ThenByDescending(n => n.NotificationId)
                .Take(100)
                .ToList();

            return View(
                "~/Views/Workspace/Notifications.cshtml",
                notifications
            );
        }

        // =========================================
        // 2. MARK MY NOTIFICATION READ / UNREAD
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetRead(
            int id,
            bool isRead,
            string? filter)
        {
            string userId = CurrentUserId();
            int[] myTaskIds = MyTaskIds(userId);

            // Must belong to a task I own
            var notification = _notificationDb.Notifications
                .FirstOrDefault(n =>
                    n.NotificationId == id &&
                    n.TaskId.HasValue &&
                    myTaskIds.Contains(n.TaskId.Value));

            if (notification == null)
            {
                return NotFound();
            }

            notification.IsRead = isRead;

            notification.ReadAt = isRead
                ? DateTime.UtcNow
                : null;

            _notificationDb.SaveChanges();

            return RedirectToAction(
                nameof(Index),
                new { filter = NormalizeFilter(filter) }
            );
        }

        // =========================================
        // 3. MARK ALL MY NOTIFICATIONS AS READ
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarkAllRead()
        {
            string userId = CurrentUserId();
            int[] myTaskIds = MyTaskIds(userId);

            // Never change another user's notifications
            var unread = _notificationDb.Notifications
                .Where(n =>
                    !n.IsRead &&
                    n.TaskId.HasValue &&
                    myTaskIds.Contains(n.TaskId.Value))
                .ToList();

            if (unread.Count > 0)
            {
                DateTime readTime = DateTime.UtcNow;

                foreach (var notification in unread)
                {
                    notification.IsRead = true;
                    notification.ReadAt = readTime;
                }

                _notificationDb.SaveChanges();
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================
        // 4. GENERATE NOTIFICATIONS FOR MY TASKS
        // =========================================

        private void SyncNotifications(
            string userId,
            int[] myTaskIds)
        {
            // Retrieve only this user's tasks
            var tasks = _taskDb.Tasks
                .AsNoTracking()
                .Where(t => myTaskIds.Contains(t.TaskId))
                .ToList();

            // Assignments must have both an owned task
            // and an owned team member
            var assignments = _teamDb.TaskAssignments
                .Include(a => a.Member)
                .AsNoTracking()
                .Where(a =>
                    myTaskIds.Contains(a.TaskId) &&
                    a.Member.OwnerUserId == userId)
                .ToList();

            // Existing event keys prevent duplicates
            var knownKeys = _notificationDb.Notifications
                .AsNoTracking()
                .Where(n =>
                    n.TaskId.HasValue &&
                    myTaskIds.Contains(n.TaskId.Value))
                .Select(n => n.EventKey)
                .ToHashSet();

            var taskTitles = tasks.ToDictionary(
                t => t.TaskId,
                t => t.Title
            );

            DateTime today = DateTime.Today;
            DateTime oldestDeadline = today.AddDays(-30);
            DateTime latestDeadline = today.AddDays(7);

            // A. Completed tasks and deadlines
            foreach (var task in tasks)
            {
                bool isCompleted =
                    task.Status == "Completed";

                if (isCompleted)
                {
                    AddNewNotification(
                        knownKeys,
                        "completed:" + task.TaskId,
                        "completed",
                        "Completed task detected",
                        "\"" + task.Title +
                        "\" is recorded as completed.",
                        task.TaskId
                    );
                }

                if (isCompleted || !task.DueDate.HasValue)
                {
                    continue;
                }

                DateTime dueDate = task.DueDate.Value.Date;

                if (dueDate < oldestDeadline ||
                    dueDate > latestDeadline)
                {
                    continue;
                }

                bool overdue = dueDate < today;

                string title = overdue
                    ? "Overdue task detected"
                    : "Upcoming task deadline";

                string message = overdue
                    ? "\"" + task.Title +
                      "\" had a due date of " +
                      dueDate.ToString("MMM dd, yyyy") + "."
                    : "\"" + task.Title +
                      "\" is due on " +
                      dueDate.ToString("MMM dd, yyyy") + ".";

                string eventKey =
                    "deadline:" +
                    task.TaskId + ":" +
                    dueDate.ToString("yyyyMMdd");

                AddNewNotification(
                    knownKeys,
                    eventKey,
                    "deadline",
                    title,
                    message,
                    task.TaskId
                );
            }

            // B. Assignment notifications
            foreach (var assignment in assignments)
            {
                if (!taskTitles.TryGetValue(
                    assignment.TaskId,
                    out string? taskTitle))
                {
                    continue;
                }

                string memberName =
                    assignment.Member?.FullName ??
                    "Team member";

                string eventKey =
                    "assignment:" +
                    assignment.TaskId + ":" +
                    assignment.MemberId;

                AddNewNotification(
                    knownKeys,
                    eventKey,
                    "assignment",
                    "Team task assignment detected",
                    "\"" + taskTitle +
                    "\" was assigned to " +
                    memberName + ".",
                    assignment.TaskId
                );
            }

            if (_notificationDb.ChangeTracker.HasChanges())
            {
                _notificationDb.SaveChanges();
            }
        }

        // =========================================
        // 5. ADD NOTIFICATION
        // =========================================

        private void AddNewNotification(
            HashSet<string> knownKeys,
            string eventKey,
            string type,
            string title,
            string message,
            int taskId)
        {
            // Avoid duplicate events
            if (!knownKeys.Add(eventKey))
            {
                return;
            }

            if (message.Length > 500)
            {
                message = message.Substring(0, 500);
            }

            var notification = new AppNotification
            {
                EventKey = eventKey,
                Type = type,
                Title = title,
                Message = message,

                // Linked to an owned Task
                TaskId = taskId,

                IsRead = false
            };

            _notificationDb.Notifications.Add(notification);
        }

        // =========================================
        // 6. ALLOWED FILTER VALUES
        // =========================================

        private static string NormalizeFilter(
            string? filter)
        {
            return filter switch
            {
                "unread" => "unread",
                "deadline" => "deadline",
                "assignment" => "assignment",
                "completed" => "completed",
                _ => "all"
            };
        }
    }
}
