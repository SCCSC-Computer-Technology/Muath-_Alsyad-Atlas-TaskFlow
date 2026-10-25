
// App #1 - Atlas TaskFlow - Team Tasks Page ViewModel

using System;
using System.Collections.Generic;
using Atlas_TaskFlow_App.Models;

namespace Atlas_TaskFlow_App.ViewModels
{
    public class TeamTaskRowViewModel
    {
        public int TaskId { get; set; }

        public string Title { get; set; } = "";

        public int ProjectId { get; set; }

        public string ProjectName { get; set; } = "";

        public DateTime? DueDate { get; set; }

        public string Priority { get; set; } = "";

        public string Status { get; set; } = "";

        public int? MemberId { get; set; }

        public string AssigneeName { get; set; } = "Unassigned";
    }

    public class TeamTasksPageViewModel
    {
        // All tasks, used for real statistics
        public List<TeamTaskRowViewModel> AllTasks { get; set; }
            = new();

        // Tasks matching current filters
        public List<TeamTaskRowViewModel> Tasks { get; set; }
            = new();

        public List<TeamMember> Members { get; set; }
            = new();

        public List<Project> Projects { get; set; }
            = new();

        public List<string> Statuses { get; set; }
            = new();

        public List<string> Priorities { get; set; }
            = new();

        public string Search { get; set; } = "";

        public int? ProjectId { get; set; }

        public int? MemberId { get; set; }

        public string Status { get; set; } = "";

        public string Priority { get; set; } = "";
    }
}
