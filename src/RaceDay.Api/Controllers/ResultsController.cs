using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    [Route("api")]
    public class ResultsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public ResultsController(RaceDayDbContext context)
        {
            _context = context;
        }

        [HttpPost("results")]
        public async Task<IActionResult> SubmitResult([FromBody] ResultSubmissionDto dto)
        {
            var role = HttpContext.Session.GetString("Role");
            var userId = HttpContext.Session.GetInt32("UserId");

            var enrolment = await _context.Enrolments
                .Include(e => e.Category)
                    .ThenInclude(c => c.Event)
                .Include(e => e.Participant)
                .FirstOrDefaultAsync(e => e.EnrolmentID == dto.EnrolmentId);

            if (enrolment == null) return NotFound(new { message = "Enrolment record not found." });

            if (role != "Organiser" || enrolment.Category.Event.OrganiserID != userId)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Forbidden. Only the event owner can submit official finish results." });

            var existingResult = await _context.Results.FirstOrDefaultAsync(r => r.EnrolmentID == dto.EnrolmentId);
            if (existingResult != null)
            {
                existingResult.FinishTime = dto.FinishTime;
                existingResult.Position = dto.Position;
                existingResult.Status = dto.Status;
            }
            else
            {
                var result = new Result
                {
                    EnrolmentID = dto.EnrolmentId,
                    FinishTime = dto.FinishTime,
                    Position = dto.Position,
                    Status = dto.Status,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Results.Add(result);
            }

            await _context.SaveChangesAsync();

            return StatusCode(StatusCodes.Status201Created, new { message = "Result recorded successfully." });
        }

        [HttpGet("results/me")]
        public async Task<IActionResult> GetMyResults()
        {
            var role = HttpContext.Session.GetString("Role");
            var userId = HttpContext.Session.GetInt32("UserId");

            if (role != "Participant" || !userId.HasValue)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Forbidden. Only participants can view their personal results." });

            var results = await _context.Results
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e.Category)
                        .ThenInclude(c => c.Event)
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e.Participant)
                .Where(r => r.Enrolment.ParticipantID == userId.Value)
                .OrderBy(r => r.Enrolment.Category.Event.EventDate)
                .Select(r => new ResultResponseDto(
                    r.ResultID,
                    r.EnrolmentID,
                    r.Enrolment.Participant.FullName,
                    r.Enrolment.BibNumber,
                    r.Enrolment.Category.Event.EventName,
                    r.Enrolment.Category.CategoryName,
                    r.FinishTime,
                    r.Position,
                    r.Status
                ))
                .ToListAsync();

            return Ok(results);
        }

        [HttpGet("events/{eventId:int}/results")]
        public async Task<IActionResult> GetEventResults(int eventId)
        {
            var results = await _context.Results
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e.Participant)
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e.Category)
                .Where(r => r.Enrolment.Category.EventID == eventId)
                .OrderBy(r => r.Position)
                .Select(r => new ResultResponseDto(
                    r.ResultID,
                    r.EnrolmentID,
                    r.Enrolment.Participant.FullName,
                    r.Enrolment.BibNumber,
                    r.Enrolment.Category.Event.EventName,
                    r.Enrolment.Category.CategoryName,
                    r.FinishTime,
                    r.Position,
                    r.Status
                ))
                .ToListAsync();

            return Ok(results);
        }
    }
}