using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace RaceDay.Mvc.Controllers
{
    public class OrganiserController : Controller
    {
        private readonly IRaceDayApiClient _apiClient;

        public OrganiserController(IRaceDayApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            if (HttpContext.Session.GetString("UserRole") != "Organizer" && HttpContext.Session.GetString("UserRole") != "Organiser")
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Events = await _apiClient.GetEventsAsync() ?? new();
            ViewBag.Profile = await _apiClient.GetProfileAsync();

            return View("~/Views/Shared/Dashboard.cshtml");
        }

        private bool IsOrganiser()
        {
            var role = HttpContext.Session.GetString("Role") ?? HttpContext.Session.GetString("UserRole");
            return role == "Organiser" || role == "Organizer";
        }

        /// <summary>
        /// Loads an event and confirms the current organiser owns it.
        /// Returns a denial result when the caller must be redirected instead.
        /// </summary>
        private async Task<(EventSummaryViewModel? Event, IActionResult? Denial)> LoadOwnedEventAsync(int eventId)
        {
            if (!IsOrganiser())
            {
                TempData["ErrorMessage"] = "Only organisers can manage events.";
                return (null, RedirectToAction("Login", "Account"));
            }

            var ev = await _apiClient.GetEventDetailsAsync(eventId);
            if (ev == null) return (null, NotFound());

            if (ev.OrganiserId != HttpContext.Session.GetInt32("UserId"))
            {
                TempData["ErrorMessage"] = "You can only manage your own events.";
                return (null, RedirectToAction(nameof(Dashboard)));
            }

            return (ev, null);
        }

        private static string? ValidateBannerImage(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowed.Contains(Path.GetExtension(file.FileName).ToLowerInvariant()))
                return "The banner must be a JPG, PNG or WebP image.";

            if (file.Length > 5 * 1024 * 1024)
                return "The banner must be 5 MB or smaller.";

            return null;
        }

        [HttpGet]
        public IActionResult CreateEvent()
        {
            if (!IsOrganiser())
            {
                TempData["ErrorMessage"] = "Only organisers can create new events.";
                return RedirectToAction("Login", "Account");
            }

            return View(new CreateEventViewModel
            {
                Categories = new List<CreateCategoryViewModel> { new() }
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateEvent(CreateEventViewModel model, IFormFile? bannerImage)
        {
            if (!IsOrganiser())
            {
                TempData["ErrorMessage"] = "Only organisers can create new events.";
                return RedirectToAction("Login", "Account");
            }

            var bannerError = ValidateBannerImage(bannerImage);
            if (bannerError != null) ModelState.AddModelError(string.Empty, bannerError);

            if (model.EventDate.Date < DateTime.UtcNow.Date)
                ModelState.AddModelError(nameof(model.EventDate), "The event date cannot be in the past.");

            if (!ModelState.IsValid) return View(model);

            var eventId = await _apiClient.CreateEventAsync(model);
            if (!eventId.HasValue)
            {
                ModelState.AddModelError(string.Empty, "Failed to create the event. Please check the details and try again.");
                return View(model);
            }

            var failedCategories = new List<string>();
            foreach (var category in model.Categories)
            {
                if (string.IsNullOrWhiteSpace(category.CategoryName)) continue;

                var (categorySaved, categoryMessage) = await _apiClient.AddCategoryAsync(eventId.Value, category);
                if (!categorySaved) failedCategories.Add($"{category.CategoryName} ({categoryMessage})");
            }

            if (bannerImage != null && bannerImage.Length > 0 &&
                !await _apiClient.UploadEventBannerAsync(eventId.Value, bannerImage))
            {
                TempData["ErrorMessage"] = "Event created, but the banner image could not be uploaded.";
                return RedirectToAction(nameof(Dashboard));
            }

            if (failedCategories.Any())
            {
                TempData["ErrorMessage"] = $"Event created, but some categories could not be saved: {string.Join("; ", failedCategories)}";
                return RedirectToAction(nameof(Dashboard));
            }

            TempData["SuccessMessage"] = "Event created successfully!";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpGet]
        public async Task<IActionResult> EditEvent(int id)
        {
            var (ev, denial) = await LoadOwnedEventAsync(id);
            if (denial != null) return denial;

            return View(new EditEventViewModel
            {
                EventID = ev!.EventID,
                EventName = ev.EventName,
                EventDate = ev.EventDate,
                StartTime = ev.StartTime,
                VenueID = ev.VenueId,
                Description = ev.Description ?? string.Empty,
                Status = ev.Status,
                BannerImageUrl = ev.BannerImageUrl
            });
        }

        [HttpPost]
        public async Task<IActionResult> EditEvent(EditEventViewModel model, IFormFile? bannerImage)
        {
            var (ev, denial) = await LoadOwnedEventAsync(model.EventID);
            if (denial != null) return denial;

            var bannerError = ValidateBannerImage(bannerImage);
            if (bannerError != null) ModelState.AddModelError(string.Empty, bannerError);

            if (!ModelState.IsValid)
            {
                model.BannerImageUrl = ev!.BannerImageUrl;
                return View(model);
            }

            var (success, message) = await _apiClient.UpdateEventAsync(model);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                model.BannerImageUrl = ev!.BannerImageUrl;
                return View(model);
            }

            if (bannerImage != null && bannerImage.Length > 0)
            {
                if (!await _apiClient.UploadEventBannerAsync(model.EventID, bannerImage))
                {
                    TempData["ErrorMessage"] = "Event updated, but the new banner could not be uploaded.";
                    return RedirectToAction(nameof(Dashboard));
                }

                TempData["SuccessMessage"] = "Event and banner updated successfully.";
                return RedirectToAction(nameof(Dashboard));
            }

            if (model.RemoveBanner)
            {
                var (bannerCleared, bannerMessage) = await _apiClient.ClearEventBannerAsync(model.EventID);
                TempData[bannerCleared ? "SuccessMessage" : "ErrorMessage"] = bannerCleared
                    ? "Event updated and banner removed."
                    : bannerMessage;

                return RedirectToAction(nameof(Dashboard));
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpGet]
        public async Task<IActionResult> ManageCategories(int id)
        {
            var (ev, denial) = await LoadOwnedEventAsync(id);
            if (denial != null) return denial;

            return View(ev);
        }

        [HttpPost]
        public async Task<IActionResult> AddCategory(int eventId, CreateCategoryViewModel model)
        {
            var (_, denial) = await LoadOwnedEventAsync(eventId);
            if (denial != null) return denial;

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Enter a category name, a distance of at least 0.1 km, a fee and a capacity of at least 1.";
                return RedirectToAction(nameof(ManageCategories), new { id = eventId });
            }

            var (success, message) = await _apiClient.AddCategoryAsync(eventId, model);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;

            return RedirectToAction(nameof(ManageCategories), new { id = eventId });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCategory(int eventId, int categoryId, CreateCategoryViewModel model)
        {
            var (_, denial) = await LoadOwnedEventAsync(eventId);
            if (denial != null) return denial;

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Category values are invalid; the distance must be at least 0.1 km and the capacity at least 1.";
                return RedirectToAction(nameof(ManageCategories), new { id = eventId });
            }

            var (success, message) = await _apiClient.UpdateCategoryAsync(categoryId, model);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;

            return RedirectToAction(nameof(ManageCategories), new { id = eventId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCategory(int eventId, int categoryId)
        {
            var (_, denial) = await LoadOwnedEventAsync(eventId);
            if (denial != null) return denial;

            var (success, message) = await _apiClient.DeleteCategoryAsync(categoryId);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;

            return RedirectToAction(nameof(ManageCategories), new { id = eventId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            if (!IsOrganiser())
            {
                TempData["ErrorMessage"] = "Only organisers can manage events.";
                return RedirectToAction("Login", "Account");
            }

            var (success, message) = await _apiClient.DeleteEventAsync(id);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;

            return RedirectToAction(nameof(Dashboard));
        }

        [HttpGet]
        public async Task<IActionResult> Participants(int id)
        {
            var (ev, denial) = await LoadOwnedEventAsync(id);
            if (denial != null) return denial;

            var enrolments = await _apiClient.GetEventEnrolmentsAsync(id);
            ViewBag.EventId = id;
            ViewBag.EventName = ev!.EventName;
            ViewBag.Results = await _apiClient.GetEventResultsAsync(id);
            return View("ManageRoster", enrolments);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitResult(int eventId, ResultSubmissionViewModel model)
        {
            var (_, denial) = await LoadOwnedEventAsync(eventId);
            if (denial != null) return denial;

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Enter a finish time such as 03:15:20 and a position of at least 1.";
                return RedirectToAction(nameof(Participants), new { id = eventId });
            }

            var (success, message) = await _apiClient.SubmitResultAsync(model);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;

            return RedirectToAction(nameof(Participants), new { id = eventId });
        }

        [HttpGet]
        public async Task<IActionResult> ExportResults(int id)
        {
            var (ev, denial) = await LoadOwnedEventAsync(id);
            if (denial != null) return denial;

            var enrolments = await _apiClient.GetEventEnrolmentsAsync(id);
            var results = await _apiClient.GetEventResultsAsync(id);

            var csv = new StringBuilder();
            csv.AppendLine("Bib,Participant,Category,Enrolment Status,Enrolled On,Finish Time,Position,Result Status");

            foreach (var enrolment in enrolments.OrderBy(e => e.BibNumber))
            {
                var result = results.FirstOrDefault(r => r.EnrolmentId == enrolment.EnrolmentID);

                csv.AppendLine(string.Join(",", new[]
                {
                    CsvField(enrolment.BibNumber),
                    CsvField(enrolment.ParticipantName),
                    CsvField(enrolment.CategoryName),
                    CsvField(enrolment.Status),
                    CsvField(enrolment.EnrolmentDate.ToString("yyyy-MM-dd HH:mm")),
                    CsvField(result == null ? string.Empty : result.FinishTime.ToString(@"hh\:mm\:ss")),
                    CsvField(result == null ? string.Empty : result.Position.ToString()),
                    CsvField(result == null ? string.Empty : result.Status)
                }));
            }

            // Byte order mark keeps Excel happy with non-ASCII names.
            var bytes = Encoding.UTF8.GetPreamble()
                .Concat(Encoding.UTF8.GetBytes(csv.ToString()))
                .ToArray();

            var fileName = Slugify(ev!.EventName) + "-results.csv";
            return File(bytes, "text/csv", fileName);
        }

        private static string CsvField(string? value)
        {
            var text = value ?? string.Empty;

            // Neutralise spreadsheet formula injection.
            if (text.Length > 0 && "=+-@".Contains(text[0]))
                text = "'" + text;

            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }

        private static string Slugify(string name)
        {
            var slug = new string(name.Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray());
            slug = slug.Trim('-');
            return slug.Length == 0 ? "event" : slug.ToLowerInvariant();
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var profile = await _apiClient.GetProfileAsync();
            return View(profile ?? new UserProfileViewModel { FullName = "Event Organiser", Email = "organiser@raceday.local" });
        }
    }
}