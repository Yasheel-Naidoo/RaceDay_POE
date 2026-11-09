using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RaceDay.Mvc.Controllers
{
    public class AccountController : Controller
    {
        private readonly IRaceDayApiClient _apiClient;

        public AccountController(IRaceDayApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        [HttpGet]
        public IActionResult Login(bool registered = false)
        {
            // Shown only when arriving straight from a successful registration.
            ViewBag.Registered = registered;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var (success, message, role, userId, fullName) = await _apiClient.LoginAsync(model);

            if (success)
            {
                // Synchronize all session keys so both API and MVC recognize the user
                HttpContext.Session.SetString("Role", role ?? "Participant");
                HttpContext.Session.SetString("UserRole", role ?? "Participant");
                HttpContext.Session.SetString("UserFullName", fullName ?? "User");
                if (userId.HasValue)
                {
                    HttpContext.Session.SetInt32("UserId", userId.Value);
                }

                return RedirectToAction("Dashboard", "Account");
            }

            ModelState.AddModelError(string.Empty, message);
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var (success, message) = await _apiClient.RegisterAsync(model);

            if (success)
            {
                return RedirectToAction("Login", new { registered = true });
            }

            ModelState.AddModelError(string.Empty, message);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var role = HttpContext.Session.GetString("Role") ?? HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(role))
            {
                return RedirectToAction("Login", "Account");
            }

            // Populate ViewBag safely based on role without causing missing view exceptions
            try
            {
                if (role == "Organiser")
                {
                    ViewBag.Events = await _apiClient.GetEventsAsync();
                }
                else
                {
                    ViewBag.Enrolments = await _apiClient.GetMyEnrolmentsAsync();
                    ViewBag.Results = await _apiClient.GetMyResultsAsync();
                    ViewBag.Profile = await _apiClient.GetProfileAsync();
                }
            }
            catch
            {
                ViewBag.Enrolments = new List<object>();
                ViewBag.Results = new List<object>();
                ViewBag.Events = new List<object>();
            }

            // Render the unified shared dashboard view safely
            return View("~/Views/Shared/Dashboard.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var profile = await _apiClient.GetProfileAsync();
            return View(profile);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _apiClient.LogoutAsync();
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}