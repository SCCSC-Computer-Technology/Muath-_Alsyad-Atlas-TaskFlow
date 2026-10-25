
// App #1 - Atlas TaskFlow - Workspace Wireframes Controller

using Microsoft.AspNetCore.Mvc;

namespace Atlas_TaskFlow_App.Controllers
{
    public class WorkspaceController : Controller
    {
        [HttpGet]
        public IActionResult TeamTasks()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Calendar()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Notifications()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Settings()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Help()
        {
            return View();
        }
    }
}
