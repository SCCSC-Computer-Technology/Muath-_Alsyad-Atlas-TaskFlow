
// App #1 - Atlas TaskFlow - Team Member Model

using System;
using System.Collections.Generic;

namespace Atlas_TaskFlow_App.Models
{
    public class TeamMember
    {
        public int MemberId { get; set; }

        public string FullName { get; set; } = "";

        public string? Email { get; set; }

        public string Role { get; set; } = "Member";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public ICollection<TaskAssignment> Assignments { get; set; }
            = new List<TaskAssignment>();
    }
}
