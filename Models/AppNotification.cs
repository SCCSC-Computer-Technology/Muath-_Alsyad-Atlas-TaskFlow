
// App #1 - Atlas TaskFlow - Notification Model

using System;

namespace Atlas_TaskFlow_App.Models
{
    public class AppNotification
    {
        public int NotificationId { get; set; }

        public string EventKey { get; set; } = "";

        public string Type { get; set; } = "";

        public string Title { get; set; } = "";

        public string Message { get; set; } = "";

        public int? TaskId { get; set; }

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? ReadAt { get; set; }
    }
}
