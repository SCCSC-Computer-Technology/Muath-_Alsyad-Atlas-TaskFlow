
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

        public ICollection<TaskItem> Tasks { get; set; }= new List<TaskItem>();
    }
}
