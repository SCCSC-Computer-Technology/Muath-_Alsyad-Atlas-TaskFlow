
// App #1 - Atlas TaskFlow
// Phase: User Settings Backend

using System;
using System.Threading.Tasks;
using Atlas_TaskFlow_App.Data;
using Atlas_TaskFlow_App.Models;
using Atlas_TaskFlow_App.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atlas_TaskFlow_App.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly UserSettingsDataContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public SettingsController(
            UserSettingsDataContext db,
            UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // ==========================================
        // 1. DISPLAY CURRENT USER SETTINGS
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var profile = await _db.UserSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    s => s.UserId == user.Id);

            // New users receive initial on-screen values.
            // No database row is created by this GET request.
            if (profile == null)
            {
                profile = new UserSettings
                {
                    UserId = user.Id,

                    FullName =
                        user.UserName?.Split('@')[0]
                        ?? "Atlas User",

                    TimeZone = "America/New_York",

                    EmailAlerts = true,
                    TaskReminders = true,
                    TeamUpdates = true,
                    WeeklySummary = false
                };
            }

            var model = new SettingsPageViewModel
            {
                Profile = profile,
                Email = user.Email ?? user.UserName ?? ""
            };

            return View(
                "~/Views/Workspace/Settings.cshtml",
                model
            );
        }

        // ==========================================
        // 2. SAVE PROFILE
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveProfile(
            string? fullName,
            string? studentId,
            string? university,
            string? major,
            string? academicYear,
            DateTime? expectedGraduation,
            string? timeZone,
            string? bio)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            fullName = fullName?.Trim();
            studentId = Clean(studentId);
            university = Clean(university);
            major = Clean(major);
            academicYear = Clean(academicYear);
            timeZone = Clean(timeZone);
            bio = Clean(bio);

            if (!ModelState.IsValid ||
                string.IsNullOrWhiteSpace(fullName) ||
                fullName.Length < 2 ||
                fullName.Length > 100 ||
                TooLong(studentId, 50) ||
                TooLong(university, 150) ||
                TooLong(major, 100) ||
                TooLong(academicYear, 30) ||
                TooLong(timeZone, 100) ||
                TooLong(bio, 500))
            {
                TempData["SettingsError"] =
                    "Please check your profile fields and their lengths.";

                return RedirectToAction(nameof(Index));
            }

            var profile = await _db.UserSettings
                .FirstOrDefaultAsync(
                    s => s.UserId == user.Id);

            if (profile == null)
            {
                profile = NewProfile(user);
                _db.UserSettings.Add(profile);
            }

            profile.FullName = fullName;
            profile.StudentId = studentId;
            profile.University = university;
            profile.Major = major;
            profile.AcademicYear = academicYear;
            profile.ExpectedGraduation = expectedGraduation;
            profile.TimeZone = timeZone;
            profile.Bio = bio;
            profile.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            TempData["SettingsSuccess"] =
                "Profile saved successfully.";

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // 3. SAVE NOTIFICATION PREFERENCES
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveNotifications(
            bool emailAlerts,
            bool taskReminders,
            bool teamUpdates,
            bool weeklySummary)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var profile = await _db.UserSettings
                .FirstOrDefaultAsync(
                    s => s.UserId == user.Id);

            if (profile == null)
            {
                profile = NewProfile(user);
                _db.UserSettings.Add(profile);
            }

            profile.EmailAlerts = emailAlerts;
            profile.TaskReminders = taskReminders;
            profile.TeamUpdates = teamUpdates;
            profile.WeeklySummary = weeklySummary;
            profile.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            TempData["SettingsSuccess"] =
                "Notification preferences saved successfully.";

            string url = Url.Action(nameof(Index), "Settings")
                ?? "/Settings";

            return Redirect(url + "#notifications");
        }

        // ==========================================
        // HELPER METHODS
        // ==========================================

        private static UserSettings NewProfile(
            IdentityUser user)
        {
            return new UserSettings
            {
                UserId = user.Id,

                FullName =
                    user.UserName?.Split('@')[0]
                    ?? "Atlas User",

                TimeZone = "America/New_York"
            };
        }

        private static string? Clean(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private static bool TooLong(
            string? value,
            int maxLength)
        {
            return value != null &&
                   value.Length > maxLength;
        }
    }
}
