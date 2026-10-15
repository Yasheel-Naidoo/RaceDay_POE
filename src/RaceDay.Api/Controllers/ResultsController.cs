using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    public class ResultsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;
        public ResultsController(RaceDayDbContext context) => _context = context;

        /// <summary>
        /// Retrieves the public leaderboard for an event.
        /// </summary>
        /// <response code="200">Public leaderboard results retrieved successfully.</response>
        [HttpGet("api/events/{eventId}/results")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLeaderboard(int eventId)
        {
            var results = await _context.Results
                .Include(r => r.Enrolment).ThenInclude(e => e!.Participant)
                .Include(r => r.Enrolment).ThenInclude(e => e!.Category)
                .Where(r => r.Enrolment!.Category!.EventID == eventId && r.Status == "Completed")
                .OrderBy(r => r.Position)
                .Select(r => new
                {
                    r.ResultID,
                    FullName = r.Enrolment!.Participant != null ? r.Enrolment.Participant.FullName : "Unknown",
                    r.Enrolment.BibNumber,
                    CategoryName = r.Enrolment.Category != null ? r.Enrolment.Category.CategoryName : "Unknown",
                    r.FinishTime,
                    r.Position
                })
                .ToListAsync();

            return Ok(results);
        }

        /// <summary>
        /// Retrieves the authenticated participant's personal race history.
        /// </summary>
        /// <response code="200">Personal results retrieved successfully.</response>
        /// <response code="401">Unauthorized session.</response>
        /// <response code="403">Requires Participant role.</response>
        [HttpGet("api/results/me")]
        [RequireRole("Participant")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMyResults()
        {
            var participantId = HttpContext.Session.GetInt32("UserId")!.Value;

            var results = await _context.Results
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e!.Category)
                        .ThenInclude(c => c!.Event)
                .Where(r => r.Enrolment!.ParticipantID == participantId)
                .Select(r => new MyResultResponseDto(
                    r.ResultID,
                    r.EnrolmentID,
                    r.Enrolment!.Category!.Event!.EventName,
                    r.Enrolment.Category.CategoryName,
                    r.Enrolment.BibNumber,
                    r.FinishTime,
                    r.Position,
                    r.Status,
                    r.CreatedAt
                ))
                .ToListAsync();

            return Ok(results);
        }

        /// <summary>
        /// Captures an athlete's finish time, position, and status.
        /// </summary>
        /// <response code="201">Result recorded successfully.</response>
        /// <response code="400">Invalid result data.</response>
        /// <response code="401">Unauthorized session.</response>
        /// <response code="403">Organiser does not own this event.</response>
        /// <response code="404">Enrolment not found.</response>
        /// <response code="409">Result already recorded for this enrolment.</response>
        [HttpPost("api/results")]
        [RequireRole("Organiser")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CaptureResult([FromBody] CaptureResultDto dto)
        {
            var enrolment = await _context.Enrolments
                .Include(e => e.Category)
                    .ThenInclude(c => c!.Event)
                .FirstOrDefaultAsync(e => e.EnrolmentID == dto.EnrolmentId);

            if (enrolment == null)
            {
                return NotFound(new { message = "Enrolment not found." });
            }

            var organiserId = HttpContext.Session.GetInt32("UserId")!.Value;
            if (enrolment.Category?.Event?.OrganiserID != organiserId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Forbidden. You do not own the event associated with this enrolment." });
            }

            var exists = await _context.Results.AnyAsync(r => r.EnrolmentID == dto.EnrolmentId);
            if (exists)
            {
                return Conflict(new { message = "Result already captured for this enrolment." });
            }

            var result = new Result
            {
                EnrolmentID = dto.EnrolmentId,
                FinishTime = dto.FinishTime,
                Position = dto.Position,
                Status = dto.Status,
                CreatedAt = DateTime.UtcNow
            };

            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            var responseDto = new ResultResponseDto(
                result.ResultID,
                result.EnrolmentID,
                result.FinishTime,
                result.Position,
                result.Status,
                result.CreatedAt
            );

            return Created($"api/results/{result.ResultID}", responseDto);
        }
    }
}