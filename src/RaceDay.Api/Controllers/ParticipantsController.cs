using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    [Route("api/participants")]
    public class ParticipantsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public ParticipantsController(RaceDayDbContext context)
        {
            _context = context;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("Role");

            if (!userId.HasValue)
                return Unauthorized(new { message = "Unauthorized user session." });

            var participant = await _context.Participants.FindAsync(userId.Value);
            if (participant == null)
                return NotFound(new { message = "Participant profile not found." });

            return Ok(new
            {
                participant.ParticipantID,
                participant.FullName,
                participant.Email,
                participant.Phone,
                participant.DateOfBirth,
                participant.EmergencyContactName,
                participant.EmergencyContactPhone
            });
        }

        [HttpGet("enrolments")]
        public async Task<IActionResult> GetEnrolments()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return Unauthorized(new { message = "Unauthorized." });

            var enrolments = await _context.Enrolments
                .Include(e => e.Category)
                    .ThenInclude(c => c.Event)
                .Where(e => e.ParticipantID == userId.Value)
                .Select(e => new
                {
                    e.EnrolmentID,
                    EventName = e.Category != null && e.Category.Event != null ? e.Category.Event.EventName : "Unknown Event",
                    CategoryName = e.Category != null ? e.Category.CategoryName : "Unknown Category",
                    e.BibNumber,
                    e.Status
                })
                .ToListAsync();

            return Ok(enrolments);
        }

        [HttpGet("results")]
        public async Task<IActionResult> GetResults()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return Unauthorized(new { message = "Unauthorized." });

            var results = await _context.Results
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e.Category)
                        .ThenInclude(c => c.Event)
                .Where(r => r.Enrolment.ParticipantID == userId.Value)
                .Select(r => new
                {
                    r.ResultID,
                    EventName = r.Enrolment != null && r.Enrolment.Category != null && r.Enrolment.Category.Event != null ? r.Enrolment.Category.Event.EventName : "Race Event",
                    CategoryName = r.Enrolment != null && r.Enrolment.Category != null ? r.Enrolment.Category.CategoryName : "Standard",
                    FinishTime = r.FinishTime.ToString(),
                    r.Position
                })
                .ToListAsync();

            return Ok(results);
        }

        [HttpPost("avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return Unauthorized(new { message = "Unauthorized." });

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded." });

            return Ok(new { message = "Avatar uploaded successfully." });
        }
    }
}