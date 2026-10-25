
// App #1 - Atlas TaskFlow - Settings ViewModel

using Atlas_TaskFlow_App.Models;

namespace Atlas_TaskFlow_App.ViewModels
{
    public class SettingsPageViewModel
    {
        public UserSettings Profile { get; set; } = new();

        public string Email { get; set; } = "";
    }
}
