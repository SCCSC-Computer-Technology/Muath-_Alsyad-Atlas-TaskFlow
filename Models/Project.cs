
// App #1 - Atlas TaskFlow
// Project Model - Project Owner

using System;
using System.Collections.Generic;

namespace Atlas_TaskFlow_App.Models
{
    public class Project
    {
        public int ProjectId { get; set; }

        public string Name { get; set; } = "";

        public string? Description { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? DueDate { get; set; }

        public string Status { get; set; } = "";

        // ASP.NET Core Identity user who owns this project
        public string OwnerUserId { get; set; } = "";

        // Tasks that belong to this project
        public ICollection<TaskItem> Tasks { get; set; }
            = new List<TaskItem>();
    }
}
