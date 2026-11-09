
// App #1 - Atlas TaskFlow
// Admin Controller - Admin Role Only

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atlas_TaskFlow_App.Controllers
{
    // Only users with the Admin role can open this controller
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;

        public AdminController(
            UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        // GET: /Admin
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Admin can see registered account information
            var users = await _userManager.Users
                .AsNoTracking()
                .OrderBy(u => u.Email)
                .ToListAsync();

            // This action does not modify user accounts
            return View(users);
        }
    }
}
