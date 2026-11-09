
// App #1 - Atlas TaskFlow
// Team Member Model - User Ownership

using System;
using System.Collections.Generic;

namespace Atlas_TaskFlow_App.Models
{
    public class TeamMember
    {
        public int MemberId { get; set; }

        public string FullName { get; set; } = "";

        public string? Email { get; set; }

        // A team label, not an Identity permission
        public string Role { get; set; } = "Member";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        // The Identity user who owns this team member
        public string OwnerUserId { get; set; } = "";

        public ICollection<TaskAssignment> Assignments { get; set; }
            = new List<TaskAssignment>();
    }
}
