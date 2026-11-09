using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    [Route("api")]
    public class EnrolmentsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public EnrolmentsController(RaceDayDbContext context)
        {
            _context = context;
        }

        [HttpPost("events/{eventId:int}/enrol")]
        public async Task<IActionResult> Enrol(int eventId, [FromBody] EnrolRequestDto dto)
        {
            var role = HttpContext.Session.GetString("Role");
            var userId = HttpContext.Session.GetInt32("UserId");

            if (role != "Participant" || !userId.HasValue)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Forbidden. This action requires the 'Participant' role." });

            var category = await _context.Categories
                .Include(c => c.Enrolments)
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryID == dto.CategoryId && c.EventID == eventId);

            if (category == null) return NotFound(new { message = "Selected race category does not exist for this event." });

            if (category.Event.EventDate.Date < DateTime.UtcNow.Date)
                return Conflict(new { message = "This event has already taken place, so enrolment is closed." });

            if (!string.Equals(category.Event.Status, "Open", StringComparison.OrdinalIgnoreCase))
                return Conflict(new { message = "Registration for this event is closed." });

            var alreadyEnrolled = await _context.Enrolments
                .AnyAsync(e => e.ParticipantID == userId.Value && e.CategoryID == dto.CategoryId && e.Status != "Withdrawn");

            if (alreadyEnrolled)
                return Conflict(new { message = "You are already enrolled in this category." });

            var activeCount = category.Enrolments.Count(e => e.Status != "Withdrawn");
            if (activeCount >= category.MaxParticipants)
                return Conflict(new { message = "Registration closed. Category capacity has been reached." });

            var randomToken = new Random().Next(1000, 9999);
            var bibNumber = $"BIB-{eventId}-{dto.CategoryId}-{randomToken}";

            var enrolment = new Enrolment
            {
                ParticipantID = userId.Value,
                CategoryID = dto.CategoryId,
                BibNumber = bibNumber,
                Status = "Confirmed",
                EnrolmentDate = DateTime.UtcNow
            };

            _context.Enrolments.Add(enrolment);
            await _context.SaveChangesAsync();

            // Return a projection: serialising the tracked entity causes a
            // Category -> Enrolments -> Category reference cycle.
            return StatusCode(StatusCodes.Status201Created, new
            {
                enrolment.EnrolmentID,
                enrolment.ParticipantID,
                enrolment.CategoryID,
                enrolment.BibNumber,
                enrolment.Status,
                enrolment.EnrolmentDate
            });
        }

        [HttpGet("enrolments/my")]
        public async Task<IActionResult> GetMyEnrolments()
        {
            var role = HttpContext.Session.GetString("Role");
            var userId = HttpContext.Session.GetInt32("UserId");

            if (role != "Participant" || !userId.HasValue)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Forbidden. Only athletes can view their enrolments." });

            var enrolments = await _context.Enrolments
                .Include(e => e.Category)
                    .ThenInclude(c => c.Event)
                .Include(e => e.Participant)
                .Where(e => e.ParticipantID == userId.Value)
                .OrderByDescending(e => e.EnrolmentDate)
                .Select(e => new EnrolmentResponseDto(
                    e.EnrolmentID,
                    e.ParticipantID,
                    e.Participant.FullName,
                    e.CategoryID,
                    e.Category.CategoryName,
                    e.Category.DistanceKm,
                    e.Category.Event.EventID,
                    e.Category.Event.EventName,
                    e.Category.Event.EventDate,
                    e.BibNumber,
                    e.Status,
                    e.EnrolmentDate
                ))
                .ToListAsync();

            return Ok(enrolments);
        }

        [HttpDelete("enrolments/{id:int}")]
        public async Task<IActionResult> Withdraw(int id)
        {
            var role = HttpContext.Session.GetString("Role");
            var userId = HttpContext.Session.GetInt32("UserId");

            var enrolment = await _context.Enrolments.FindAsync(id);
            if (enrolment == null) return NotFound(new { message = "Enrolment record not found." });

            if (role != "Participant" || enrolment.ParticipantID != userId)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Forbidden. You can only withdraw your own registrations." });

            enrolment.Status = "Withdrawn";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Enrolment withdrawn." });
        }
    }
}