using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;
        private readonly IBlobService _blobService;

        public EventsController(RaceDayDbContext context, IBlobService blobService)
        {
            _context = context;
            _blobService = blobService;
        }

        [HttpGet]
        public async Task<IActionResult> GetEvents([FromQuery] string? search = null)
        {
            var today = DateTime.UtcNow.Date;

            var query = _context.Events
                .Include(e => e.Organiser)
                .Include(e => e.Venue)
                .Include(e => e.Categories)
                    .ThenInclude(c => c.Enrolments)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(e => e.EventName.ToLower().Contains(search) ||
                                         e.Venue.City.ToLower().Contains(search) ||
                                         e.Categories.Any(c => c.CategoryName.ToLower().Contains(search)));
            }

            var events = await query
                .OrderBy(e => e.EventDate)
                .Select(e => new EventResponseDto(
                    e.EventID,
                    e.EventName,
                    e.EventDate,
                    e.StartTime,
                    e.EventDate < today ? "Closed" : e.Status,
                    e.Description,
                    e.BannerImageUrl,
                    e.OrganiserID,
                    e.Organiser.FullName,
                    e.VenueID,
                    e.Venue.VenueName,
                    e.Venue.City,
                    e.Venue.Province,
                    e.Categories.Select(c => new CategoryResponseDto(
                        c.CategoryID,
                        c.EventID,
                        c.CategoryName,
                        c.DistanceKm,
                        c.EntryFee,
                        c.MaxParticipants,
                        c.Enrolments.Count(en => en.Status != "Withdrawn")
                    )).ToList()
                ))
                .ToListAsync();

            return Ok(events);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetEventById(int id)
        {
            var ev = await _context.Events
                .Include(e => e.Organiser)
                .Include(e => e.Venue)
                .Include(e => e.Categories)
                    .ThenInclude(c => c.Enrolments)
                .FirstOrDefaultAsync(e => e.EventID == id);

            if (ev == null) return NotFound(new { message = "Event not found." });

            var dto = new EventResponseDto(
                ev.EventID,
                ev.EventName,
                ev.EventDate,
                ev.StartTime,
                ev.EventDate.Date < DateTime.UtcNow.Date ? "Closed" : ev.Status,
                ev.Description,
                ev.BannerImageUrl,
                ev.OrganiserID,
                ev.Organiser.FullName,
                ev.VenueID,
                ev.Venue.VenueName,
                ev.Venue.City,
                ev.Venue.Province,
                ev.Categories.Select(c => new CategoryResponseDto(
                    c.CategoryID,
                    c.EventID,
                    c.CategoryName,
                    c.DistanceKm,
                    c.EntryFee,
                    c.MaxParticipants,
                    c.Enrolments.Count(en => en.Status != "Withdrawn")
                )).ToList()
            );

            return Ok(dto);
        }

        [HttpPost]
        public async Task<IActionResult> CreateEvent([FromBody] CreateEventDto dto)
        {
            var role = HttpContext.Session.GetString("Role");
            var userId = HttpContext.Session.GetInt32("UserId");

            if (role != "Organiser" || !userId.HasValue)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Forbidden. This action requires the 'Organiser' role." });

            if (dto.EventDate.Date < DateTime.UtcNow.Date)
                return BadRequest(new { message = "The event date cannot be in the past." });

            var ev = new Event
            {
                EventName = dto.EventName,
                EventDate = dto.EventDate,
                StartTime = dto.StartTime,
                VenueID = dto.VenueId,
                Description = dto.Description,
                OrganiserID = userId.Value,
                Status = "Open",
                CreatedAt = DateTime.UtcNow
            };

            _context.Events.Add(ev);
            await _context.SaveChangesAsync();

            var responseDto = new EventResponseDto(
                ev.EventID,
                ev.EventName,
                ev.EventDate,
                ev.StartTime,
                ev.Status,
                ev.Description,
                ev.BannerImageUrl,
                ev.OrganiserID,
                "Organiser",
                ev.VenueID,
                "Venue",
                "City",
                "Province",
                new List<CategoryResponseDto>()
            );

            return StatusCode(StatusCodes.Status201Created, responseDto);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] UpdateEventDto dto)
        {
            var role = HttpContext.Session.GetString("Role");
            var userId = HttpContext.Session.GetInt32("UserId");

            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound(new { message = "Event not found." });

            if (role != "Organiser" || ev.OrganiserID != userId)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Forbidden. You can only modify your own events." });

            ev.EventName = dto.EventName;
            ev.EventDate = dto.EventDate;
            ev.StartTime = dto.StartTime;
            ev.VenueID = dto.VenueId;
            ev.Description = dto.Description;
            ev.Status = dto.Status;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Event updated successfully." });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var role = HttpContext.Session.GetString("Role");
            var userId = HttpContext.Session.GetInt32("UserId");

            var ev = await _context.Events
                .Include(e => e.Categories)
                    .ThenInclude(c => c.Enrolments)
                .FirstOrDefaultAsync(e => e.EventID == id);

            if (ev == null) return NotFound(new { message = "Event not found." });

            if (role != "Organiser" || ev.OrganiserID != userId)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Forbidden. You can only delete your own events." });

            if (ev.Categories.Any(c => c.Enrolments.Any()))
                return Conflict(new { message = "This event already has enrolments, so it cannot be deleted." });

            var bannerUrl = ev.BannerImageUrl;

            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(bannerUrl) &&
                bannerUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            {
                await _blobService.DeleteFileAsync(bannerUrl);
            }

            return Ok(new { message = "Event deleted." });
        }

        [HttpGet("{id:int}/enrolments")]
        public async Task<IActionResult> GetEventEnrolments(int id)
        {
            var role = HttpContext.Session.GetString("Role");
            var userId = HttpContext.Session.GetInt32("UserId");

            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound(new { message = "Event not found." });

            if (role != "Organiser" || ev.OrganiserID != userId)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Forbidden. You can only view participant rosters for your own events." });

            var enrolments = await _context.Enrolments
                .Include(e => e.Participant)
                .Include(e => e.Category)
                .Where(e => e.Category.EventID == id)
                .Select(e => new EnrolmentResponseDto(
                    e.EnrolmentID,
                    e.ParticipantID,
                    e.Participant.FullName,
                    e.CategoryID,
                    e.Category.CategoryName,
                    e.Category.DistanceKm,
                    ev.EventID,
                    ev.EventName,
                    ev.EventDate,
                    e.BibNumber,
                    e.Status,
                    e.EnrolmentDate
                ))
                .ToListAsync();

            return Ok(enrolments);
        }
    }
}