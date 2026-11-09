using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;
using System.Threading.Tasks;

namespace RaceDay.Mvc.Controllers
{
	public class EventController : Controller
	{
		private readonly IRaceDayApiClient _apiClient;

		public EventController(IRaceDayApiClient apiClient)
		{
			_apiClient = apiClient;
		}

		[HttpGet]
		public async Task<IActionResult> Index(string? search)
		{
			var events = await _apiClient.GetEventsAsync(search);
			ViewData["SearchQuery"] = search;
			return View(events);
		}

		[HttpGet]
		public async Task<IActionResult> Catalogue(string? search)
		{
			var events = await _apiClient.GetEventsAsync(search);
			ViewData["SearchQuery"] = search;
			return View(events);
		}

		[HttpGet]
		public async Task<IActionResult> Details(int id)
		{
			var eventDetails = await _apiClient.GetEventDetailsAsync(id);
			if (eventDetails == null) return NotFound();
			return View(eventDetails);
		}

		[HttpGet]
		public IActionResult Create()
		{
			var role = HttpContext.Session.GetString("Role") ?? HttpContext.Session.GetString("UserRole");
			if (role != "Organiser" && role != "Organizer")
			{
				TempData["ErrorMessage"] = "Only organisers can create new events.";
				return RedirectToAction("Index");
			}

			// The create-event form lives on the Organiser controller (it also handles the banner upload).
			return RedirectToAction("CreateEvent", "Organiser");
		}

		[HttpPost]
		public async Task<IActionResult> Create(CreateEventViewModel model)
		{
			var role = HttpContext.Session.GetString("Role") ?? HttpContext.Session.GetString("UserRole");
			var userId = HttpContext.Session.GetInt32("UserId");

			if ((role != "Organiser" && role != "Organizer") || !userId.HasValue)
			{
				return Unauthorized(new { message = "Forbidden. Organiser role required." });
			}

			if (!ModelState.IsValid)
				return View(model);

			var eventId = await _apiClient.CreateEventAsync(model);
			if (eventId.HasValue && eventId.Value > 0)
			{
				return RedirectToAction("Index");
			}

			ModelState.AddModelError(string.Empty, "Failed to create event. Please verify your input details.");
			return View(model);
		}

		[HttpPost]
		public async Task<IActionResult> Enrol(int eventId, int categoryId)
		{
			var role = HttpContext.Session.GetString("Role") ?? HttpContext.Session.GetString("UserRole");
			var userId = HttpContext.Session.GetInt32("UserId");

			if (string.IsNullOrEmpty(role) || !userId.HasValue)
			{
				TempData["ErrorMessage"] = "You must be logged in as a participant to enrol in a race.";
				return RedirectToAction("Login", "Account");
			}

			if (role != "Participant")
			{
				TempData["ErrorMessage"] = "Only participants can enrol in a race.";
				return RedirectToAction("Details", new { id = eventId });
			}

			var (success, message) = await _apiClient.EnrolAsync(eventId, categoryId);

			if (success)
			{
				TempData["SuccessMessage"] = message;
				return RedirectToAction("Dashboard", "Account");
			}

			TempData["ErrorMessage"] = message;
			return RedirectToAction("Details", new { id = eventId });
		}
	}
}