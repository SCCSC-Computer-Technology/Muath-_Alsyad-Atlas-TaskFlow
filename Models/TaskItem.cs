
// App #1 - Atlas TaskFlow - Task Model

using System;

namespace Atlas_TaskFlow_App.Models
{
    public class TaskItem
    {
        public int TaskId { get; set; }

        public string Title { get; set; } = "";

        public string? Description { get; set; }

        public DateTime? DueDate { get; set; }

        public string Priority { get; set; } = "";

        public string Status { get; set; } = "";

        public int ProjectId { get; set; }

        public Project? Project { get; set; }
    }
}
