
// App #1 - Atlas TaskFlow Dashboard ViewModel

namespace Atlas_TaskFlow_App.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalTasks { get; set; }

        public int OverdueTasks { get; set; }

        public int DueThisWeek { get; set; }

        public int ActiveProjects { get; set; }

        public List<DashboardTaskRow> UpcomingTasks{ get; set; } = new();
    }

    public class DashboardTaskRow
    {
        public string Title { get; set; } = "";

        public string ProjectName { get; set; } = "";

        public DateTime? DueDate { get; set; }

        public string Priority { get; set; } = "";

        public string Status { get; set; } = "";
    }
}
