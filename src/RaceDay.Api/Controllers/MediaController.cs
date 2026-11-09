using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MediaController : ControllerBase
    {
        private readonly IBlobService _blobService;
        private readonly RaceDayDbContext _context;

        public MediaController(IBlobService blobService, RaceDayDbContext context)
        {
            _blobService = blobService;
            _context = context;
        }

        [HttpPost("upload-event-banner/{eventId:int}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadEventBanner(int eventId, [FromForm] IFormFile file)
        {
            var role = HttpContext.Session.GetString("Role");
            var userId = HttpContext.Session.GetInt32("UserId");

            var raceEvent = await _context.Events.FindAsync(eventId);
            if (raceEvent == null) return NotFound(new { message = "Event not found." });

            if (role != "Organiser" || raceEvent.OrganiserID != userId)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only upload banners for events you own." });

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                return BadRequest(new { message = "Unsupported image type." });

            var previousUrl = raceEvent.BannerImageUrl;

            var bannerUrl = await _blobService.UploadFileAsync(file, "event-banners");
            raceEvent.BannerImageUrl = bannerUrl;
            await _context.SaveChangesAsync();

            // The replaced banner is no longer referenced by anything.
            if (!string.IsNullOrEmpty(previousUrl) &&
                previousUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase) &&
                previousUrl != bannerUrl)
            {
                await _blobService.DeleteFileAsync(previousUrl);
            }

            return Ok(new { bannerImageUrl = bannerUrl });
        }

        [HttpDelete("event-banner/{eventId:int}")]
        public async Task<IActionResult> ClearEventBanner(int eventId)
        {
            var role = HttpContext.Session.GetString("Role");
            var userId = HttpContext.Session.GetInt32("UserId");

            var raceEvent = await _context.Events.FindAsync(eventId);
            if (raceEvent == null) return NotFound(new { message = "Event not found." });

            if (role != "Organiser" || raceEvent.OrganiserID != userId)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only manage banners for events you own." });

            var previousUrl = raceEvent.BannerImageUrl;
            if (string.IsNullOrEmpty(previousUrl))
                return Ok(new { message = "This event has no banner to remove." });

            raceEvent.BannerImageUrl = null;
            await _context.SaveChangesAsync();

            if (previousUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            {
                await _blobService.DeleteFileAsync(previousUrl);
            }

            return Ok(new { message = "Event banner removed." });
        }

        [HttpPost("upload-profile-picture")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadProfilePicture([FromForm] IFormFile file)
        {
            var role = HttpContext.Session.GetString("Role");
            var userId = HttpContext.Session.GetInt32("UserId");

            if (role != "Participant" || !userId.HasValue)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Only participants can update athlete profile avatars." });

            var participant = await _context.Participants.FindAsync(userId.Value);
            if (participant == null) return NotFound(new { message = "Participant profile not found." });

            var avatarUrl = await _blobService.UploadFileAsync(file, "profile-avatars");
            participant.ProfilePictureUrl = avatarUrl;
            await _context.SaveChangesAsync();

            return Ok(new { profilePictureUrl = avatarUrl });
        }
    }
}