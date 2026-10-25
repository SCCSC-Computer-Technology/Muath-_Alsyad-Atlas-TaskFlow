
// App #1 - Atlas TaskFlow
// Phase: Persistent Notifications Backend

using System;
using System.Collections.Generic;
using System.Linq;
using Atlas_TaskFlow_App.Data;
using Atlas_TaskFlow_App.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atlas_TaskFlow_App.Controllers
{
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
        // 1. SHOW NOTIFICATIONS
        // =========================================

        [HttpGet]
        public IActionResult Index(string? filter)
        {
            // For this MVP, discover new notifications
            // from the current SQL Server records.
            SyncNotifications();

            filter = NormalizeFilter(filter);

            var query = _notificationDb.Notifications
                .AsNoTracking()
                .AsQueryable();

            // Statistics from the actual database
            ViewBag.TotalCount =
                _notificationDb.Notifications.Count();

            ViewBag.UnreadCount =
                _notificationDb.Notifications.Count(
                    n => !n.IsRead);

            ViewBag.DeadlineCount =
                _notificationDb.Notifications.Count(
                    n => n.Type == "deadline");

            ViewBag.AssignmentCount =
                _notificationDb.Notifications.Count(
                    n => n.Type == "assignment");

            ViewBag.CompletedCount =
                _notificationDb.Notifications.Count(
                    n => n.Type == "completed");

            ViewBag.Filter = filter;

            // Apply selected filter
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
        // 2. MARK ONE NOTIFICATION READ / UNREAD
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetRead(
            int id,
            bool isRead,
            string? filter)
        {
            var notification =
                _notificationDb.Notifications.Find(id);

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
        // 3. MARK ALL NOTIFICATIONS AS READ
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarkAllRead()
        {
            var unread =
                _notificationDb.Notifications
                    .Where(n => !n.IsRead)
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
        // 4. SYNCHRONIZE DATABASE-DERIVED ALERTS
        // =========================================

        private void SyncNotifications()
        {
            // These are real tasks from dbo.Tasks.
            var tasks = _taskDb.Tasks
                .AsNoTracking()
                .ToList();

            // These are real assignments and members.
            var assignments = _teamDb.TaskAssignments
                .Include(a => a.Member)
                .AsNoTracking()
                .ToList();

            // Existing keys prevent repeat notifications.
            var knownKeys = _notificationDb.Notifications
                .AsNoTracking()
                .Select(n => n.EventKey)
                .ToHashSet();

            var taskTitles = tasks.ToDictionary(
                t => t.TaskId,
                t => t.Title
            );

            DateTime today = DateTime.Today;

            DateTime oldestDeadline =
                today.AddDays(-30);

            DateTime latestDeadline =
                today.AddDays(7);

            // A. Task deadlines and completed tasks
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

                // Deadline alerts for non-completed tasks
                if (isCompleted || !task.DueDate.HasValue)
                {
                    continue;
                }

                DateTime dueDate =
                    task.DueDate.Value.Date;

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

            // B. Task assignments
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

            // Persist newly discovered notifications
            if (_notificationDb.ChangeTracker.HasChanges())
            {
                _notificationDb.SaveChanges();
            }
        }

        // =========================================
        // 5. ADD NOTIFICATION ONLY IF NOT EXISTS
        // =========================================

        private void AddNewNotification(
            HashSet<string> knownKeys,
            string eventKey,
            string type,
            string title,
            string message,
            int? taskId)
        {
            if (!knownKeys.Add(eventKey))
            {
                return;
            }

            // Respect SQL column lengths
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
                TaskId = taskId,
                IsRead = false
            };

            _notificationDb.Notifications.Add(
                notification
            );
        }

        // =========================================
        // 6. ALLOWED FILTERS
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
