
// App #1 - Atlas TaskFlow - User Settings Model

using System;

namespace Atlas_TaskFlow_App.Models
{
    public class UserSettings
    {
        public string UserId { get; set; } = "";

        public string FullName { get; set; } = "";

        public string? StudentId { get; set; }

        public string? University { get; set; }

        public string? Major { get; set; }

        public string? AcademicYear { get; set; }

        public DateTime? ExpectedGraduation { get; set; }

        public string? TimeZone { get; set; }

        public string? Bio { get; set; }

        public bool EmailAlerts { get; set; } = true;

        public bool TaskReminders { get; set; } = true;

        public bool TeamUpdates { get; set; } = true;

        public bool WeeklySummary { get; set; } = false;

        public DateTime UpdatedAt { get; set; }
    }
}
