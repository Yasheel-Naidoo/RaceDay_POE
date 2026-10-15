using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    public class EnrolmentsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;
        public EnrolmentsController(RaceDayDbContext context) => _context = context;

        [HttpGet("api/enrolments")]
        [RequireAuth]
        public async Task<IActionResult> GetEnrolments([FromQuery] int? eventId)
        {
            var userId = HttpContext.Session.GetInt32("UserId")!.Value;
            var role = HttpContext.Session.GetString("Role")!;

            if (role == "Participant")
            {
                var myEnrolments = await _context.Enrolments
                    .Include(e => e.Category).ThenInclude(c => c!.Event)
                    .Where(e => e.ParticipantID == userId).ToListAsync();
                return Ok(myEnrolments);
            }

            var query = _context.Enrolments
                .Include(e => e.Participant)
                .Include(e => e.Category).ThenInclude(c => c!.Event).AsQueryable();

            if (eventId.HasValue)
                query = query.Where(e => e.Category!.EventID == eventId.Value && e.Category.Event!.OrganiserID == userId);
            else
                query = query.Where(e => e.Category!.Event!.OrganiserID == userId);

            return Ok(await query.ToListAsync());
        }

        [HttpPost("api/events/{eventId}/enrol")]
        [RequireRole("Participant")]
        public async Task<IActionResult> Enrol(int eventId, [FromBody] EnrolRequestDto dto)
        {
            var participantId = HttpContext.Session.GetInt32("UserId")!.Value;

            var category = await _context.Categories.FirstOrDefaultAsync(c => c.CategoryID == dto.CategoryId && c.EventID == eventId);
            if (category == null) return NotFound(new { message = "Category does not exist for this event." });

            var alreadyEnrolled = await _context.Enrolments.AnyAsync(e => e.ParticipantID == participantId && e.CategoryID == dto.CategoryId);
            if (alreadyEnrolled) return Conflict(new { message = "You are already enrolled in this category." });

            var currentCount = await _context.Enrolments.CountAsync(e => e.CategoryID == dto.CategoryId && e.Status == "Confirmed");
            if (currentCount >= category.MaxParticipants) return Conflict(new { message = "Category capacity is full." });

            var bibNumber = $"BIB-{eventId}-{dto.CategoryId}-{new Random().Next(1000, 9999)}";

            var enrolment = new Enrolment
            {
                ParticipantID = participantId,
                CategoryID = dto.CategoryId,
                BibNumber = bibNumber,
                Status = "Confirmed"
            };

            _context.Enrolments.Add(enrolment);
            await _context.SaveChangesAsync();
            return StatusCode(StatusCodes.Status201Created, enrolment);
        }

        [HttpDelete("api/enrolments/{id}")]
        [RequireRole("Participant")]
        public async Task<IActionResult> Withdraw(int id)
        {
            var participantId = HttpContext.Session.GetInt32("UserId")!.Value;
            var enrolment = await _context.Enrolments.Include(e => e.Result).FirstOrDefaultAsync(e => e.EnrolmentID == id);
            if (enrolment == null) return NotFound();

            if (enrolment.ParticipantID != participantId) return StatusCode(403, new { message = "Forbidden." });
            if (enrolment.Result != null) return Conflict(new { message = "Cannot withdraw an enrolment that already has results." });

            enrolment.Status = "Withdrawn";
            await _context.SaveChangesAsync();
            return Ok(new { message = "Enrolment withdrawn." });
        }
    }
}