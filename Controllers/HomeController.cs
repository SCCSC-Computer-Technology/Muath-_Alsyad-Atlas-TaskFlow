
// App #1 - Atlas TaskFlow
// Home Controller - Login and Member Role

using System;
using System.Diagnostics;
using Atlas_TaskFlow_App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Atlas_TaskFlow_App.Controllers
{
    public class HomeController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public HomeController(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        // =====================================
        // 1. SHOW ATLAS LOGIN
        // =====================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return View();
        }

        // =====================================
        // 2. CHECK LOGIN
        // =====================================

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            string email,
            string password,
            bool rememberMe)
        {
            ViewData["Email"] = email;

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                ViewData["LoginError"] =
                    "Please enter your email and password.";

                return View("Index");
            }

            // Find user in AspNetUsers
            var user = await _userManager.FindByEmailAsync(
                email.Trim()
            );

            if (user == null)
            {
                ViewData["LoginError"] =
                    "Invalid email or password.";

                return View("Index");
            }

            // Verify password using Identity
            var result = await _signInManager.PasswordSignInAsync(
                user,
                password,
                rememberMe,
                lockoutOnFailure: true
            );

            if (result.Succeeded)
            {
                // New users without roles become Members
                var userRoles =
                    await _userManager.GetRolesAsync(user);

                if (userRoles.Count == 0)
                {
                    var roleResult =
                        await _userManager.AddToRoleAsync(
                            user, "Member"
                        );

                    if (!roleResult.Succeeded)
                    {
                        await _signInManager.SignOutAsync();

                        ViewData["LoginError"] =
                            "Could not set up your account role.";

                        return View("Index");
                    }

                    // Update the cookie with the new role
                    await _signInManager.RefreshSignInAsync(user);
                }

                return RedirectToAction("Index", "Dashboard");
            }

            if (result.IsNotAllowed)
            {
                ViewData["LoginError"] =
                    "Your account is not confirmed. Please confirm your email first.";
            }
            else if (result.IsLockedOut)
            {
                ViewData["LoginError"] =
                    "Your account is temporarily locked. Please try again later.";
            }
            else if (result.RequiresTwoFactor)
            {
                ViewData["LoginError"] =
                    "Two-factor sign-in is required. Use the standard Identity login page.";
            }
            else
            {
                ViewData["LoginError"] =
                    "Invalid email or password.";
            }

            return View("Index");
        }

        // =====================================
        // 3. ACCESS DENIED
        // =====================================

        [Authorize]
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return Content(
                "Access Denied. You need Admin permission to open this page."
            );
        }

        // =====================================
        // 4. PRIVACY PAGE
        // =====================================

        public IActionResult Privacy()
        {
            return View();
        }

        // =====================================
        // 5. ERROR PAGE
        // =====================================

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id
                    ?? HttpContext.TraceIdentifier
            });
        }
    }
}
