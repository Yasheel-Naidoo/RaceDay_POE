using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;
        public EventsController(RaceDayDbContext context) => _context = context;

        /// <summary>
        /// Lists all upcoming events with optional status filtering.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetEvents([FromQuery] string? status)
        {
            var query = _context.Events.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(e => e.Status == status);
            }

            var events = await query
                .Select(e => new EventResponseDto(
                    e.EventID,
                    e.EventName,
                    e.EventDate,
                    e.StartTime,
                    e.Description,
                    e.Status,
                    e.OrganiserID,
                    e.CreatedAt,
                    e.Venue == null ? null : new VenueResponseDto(
                        e.Venue.VenueID,
                        e.Venue.VenueName,
                        e.Venue.Address,
                        e.Venue.City,
                        e.Venue.Province,
                        e.Venue.PostalCode,
                        e.Venue.RouteDescription
                    ),
                    e.Categories.Select(c => new CategoryResponseDto(
                        c.CategoryID,
                        c.CategoryName,
                        c.DistanceKm,
                        c.EntryFee,
                        c.MaxParticipants
                    )).ToList()
                ))
                .ToListAsync();

            return Ok(events);
        }

        /// <summary>
        /// Returns details of a specific event by ID.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEventById(int id)
        {
            var ev = await _context.Events
                .Where(e => e.EventID == id)
                .Select(e => new EventResponseDto(
                    e.EventID,
                    e.EventName,
                    e.EventDate,
                    e.StartTime,
                    e.Description,
                    e.Status,
                    e.OrganiserID,
                    e.CreatedAt,
                    e.Venue == null ? null : new VenueResponseDto(
                        e.Venue.VenueID,
                        e.Venue.VenueName,
                        e.Venue.Address,
                        e.Venue.City,
                        e.Venue.Province,
                        e.Venue.PostalCode,
                        e.Venue.RouteDescription
                    ),
                    e.Categories.Select(c => new CategoryResponseDto(
                        c.CategoryID,
                        c.CategoryName,
                        c.DistanceKm,
                        c.EntryFee,
                        c.MaxParticipants
                    )).ToList()
                ))
                .FirstOrDefaultAsync();

            return ev == null ? NotFound(new { message = "Event not found." }) : Ok(ev);
        }

        /// <summary>
        /// Creates a new event owned by the logged-in organiser.
        /// </summary>
        [HttpPost]
        [RequireRole("Organiser")]
        public async Task<IActionResult> CreateEvent([FromBody] CreateEventDto dto)
        {
            var organiserId = HttpContext.Session.GetInt32("UserId")!.Value;

            var venueExists = await _context.Venues.AnyAsync(v => v.VenueID == dto.VenueId);
            if (!venueExists) return BadRequest(new { message = "Specified Venue ID does not exist." });

            var ev = new Event
            {
                EventName = dto.EventName,
                EventDate = dto.EventDate,
                StartTime = dto.StartTime,
                VenueID = dto.VenueId,
                Description = dto.Description,
                OrganiserID = organiserId,
                Status = "Open"
            };

            _context.Events.Add(ev);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEventById), new { id = ev.EventID }, new EventResponseDto(
                ev.EventID,
                ev.EventName,
                ev.EventDate,
                ev.StartTime,
                ev.Description,
                ev.Status,
                ev.OrganiserID,
                ev.CreatedAt,
                null,
                new List<CategoryResponseDto>()
            ));
        }

        /// <summary>
        /// Updates an existing event owned by the organiser.
        /// </summary>
        [HttpPut("{id}")]
        [RequireRole("Organiser")]
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] UpdateEventDto dto)
        {
            var organiserId = HttpContext.Session.GetInt32("UserId")!.Value;
            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound(new { message = "Event not found." });

            if (ev.OrganiserID != organiserId)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You are not the owner of this event." });

            if (dto.EventName != null) ev.EventName = dto.EventName;
            if (dto.EventDate.HasValue) ev.EventDate = dto.EventDate.Value;
            if (dto.StartTime.HasValue) ev.StartTime = dto.StartTime;
            if (dto.Description != null) ev.Description = dto.Description;
            if (dto.Status != null) ev.Status = dto.Status;

            await _context.SaveChangesAsync();
            return Ok(ev);
        }

        /// <summary>
        /// Deletes an event provided it has no enrolments.
        /// </summary>
        [HttpDelete("{id}")]
        [RequireRole("Organiser")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var organiserId = HttpContext.Session.GetInt32("UserId")!.Value;
            var ev = await _context.Events.Include(e => e.Categories).FirstOrDefaultAsync(e => e.EventID == id);
            if (ev == null) return NotFound(new { message = "Event not found." });

            if (ev.OrganiserID != organiserId)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You are not the owner of this event." });

            var categoryIds = ev.Categories.Select(c => c.CategoryID).ToList();
            var hasEnrolments = await _context.Enrolments.AnyAsync(e => categoryIds.Contains(e.CategoryID));
            if (hasEnrolments)
                return Conflict(new { message = "Cannot delete an event with existing enrolments. Cancel it instead." });

            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}