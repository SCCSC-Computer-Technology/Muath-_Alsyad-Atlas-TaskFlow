
// App #1 - Atlas TaskFlow - Task Assignment Model

using System;

namespace Atlas_TaskFlow_App.Models
{
    public class TaskAssignment
    {
        public int TaskId { get; set; }

        public int MemberId { get; set; }

        public DateTime AssignedAt { get; set; }

        public TeamMember Member { get; set; } = null!;
    }
}
