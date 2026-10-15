using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly RaceDayDbContext _context;
        public CategoriesController(RaceDayDbContext context) => _context = context;

        /// <summary>
        /// Lists all entry categories for a given event.
        /// </summary>
        /// <response code="200">List of categories returned successfully.</response>
        /// <response code="404">Event not found.</response>
        [HttpGet("api/events/{id}/categories")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCategories(int id)
        {
            var exists = await _context.Events.AnyAsync(e => e.EventID == id);
            if (!exists)
            {
                return NotFound(new { message = "Event not found." });
            }

            var categories = await _context.Categories
                .Where(c => c.EventID == id)
                .Select(c => new CategoryResponseDto(
                    c.CategoryID,
                    c.CategoryName,
                    c.DistanceKm,
                    c.EntryFee,
                    c.MaxParticipants
                ))
                .ToListAsync();

            return Ok(categories);
        }

        /// <summary>
        /// Adds a new category option to an event owned by the organiser.
        /// </summary>
        /// <response code="201">Category created successfully.</response>
        /// <response code="400">Invalid category details.</response>
        /// <response code="401">Unauthorized session.</response>
        /// <response code="403">Logged-in organiser does not own this event.</response>
        /// <response code="404">Event not found.</response>
        /// <response code="409">Category name already exists in this event.</response>
        [HttpPost("api/events/{id}/categories")]
        [RequireRole("Organiser")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AddCategory(int id, [FromBody] CreateCategoryDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CategoryName) || dto.MaxParticipants <= 0 || dto.EntryFee < 0)
            {
                return BadRequest(new { message = "Invalid category parameters. Fee must be positive, capacity > 0, and name provided." });
            }

            var organiserId = HttpContext.Session.GetInt32("UserId")!.Value;
            var ev = await _context.Events.FindAsync(id);
            if (ev == null)
            {
                return NotFound(new { message = "Event not found." });
            }

            if (ev.OrganiserID != organiserId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Forbidden. You do not own this event." });
            }

            var dup = await _context.Categories.AnyAsync(c => c.EventID == id && c.CategoryName == dto.CategoryName);
            if (dup)
            {
                return Conflict(new { message = "Category name already exists in this event." });
            }

            var cat = new Category
            {
                EventID = id,
                CategoryName = dto.CategoryName,
                DistanceKm = dto.DistanceKm,
                EntryFee = dto.EntryFee,
                MaxParticipants = dto.MaxParticipants
            };

            _context.Categories.Add(cat);
            await _context.SaveChangesAsync();

            var responseDto = new CategoryResponseDto(
                cat.CategoryID,
                cat.CategoryName,
                cat.DistanceKm,
                cat.EntryFee,
                cat.MaxParticipants
            );

            return Created($"api/categories/{cat.CategoryID}", responseDto);
        }

        /// <summary>
        /// Updates a category fee, distance, or maximum capacity.
        /// </summary>
        /// <response code="200">Category updated successfully.</response>
        /// <response code="401">Unauthorized session.</response>
        /// <response code="403">Logged-in organiser does not own the parent event.</response>
        /// <response code="404">Category not found.</response>
        [HttpPut("api/categories/{id}")]
        [RequireRole("Organiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryDto dto)
        {
            var cat = await _context.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id);
            if (cat == null)
            {
                return NotFound(new { message = "Category not found." });
            }

            var organiserId = HttpContext.Session.GetInt32("UserId")!.Value;
            if (cat.Event?.OrganiserID != organiserId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Forbidden. You do not own the parent event of this category." });
            }

            if (dto.CategoryName != null) cat.CategoryName = dto.CategoryName;
            if (dto.DistanceKm.HasValue) cat.DistanceKm = dto.DistanceKm;
            if (dto.EntryFee.HasValue) cat.EntryFee = dto.EntryFee.Value;
            if (dto.MaxParticipants.HasValue) cat.MaxParticipants = dto.MaxParticipants.Value;

            await _context.SaveChangesAsync();

            var responseDto = new CategoryResponseDto(
                cat.CategoryID,
                cat.CategoryName,
                cat.DistanceKm,
                cat.EntryFee,
                cat.MaxParticipants
            );

            return Ok(responseDto);
        }

        /// <summary>
        /// Removes a category provided it has no participant enrolments.
        /// </summary>
        /// <response code="204">Category deleted successfully.</response>
        /// <response code="401">Unauthorized session.</response>
        /// <response code="403">Logged-in organiser does not own the parent event.</response>
        /// <response code="404">Category not found.</response>
        /// <response code="409">Cannot delete a category that already has enrolments.</response>
        [HttpDelete("api/categories/{id}")]
        [RequireRole("Organiser")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var cat = await _context.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id);
            if (cat == null)
            {
                return NotFound(new { message = "Category not found." });
            }

            var organiserId = HttpContext.Session.GetInt32("UserId")!.Value;
            if (cat.Event?.OrganiserID != organiserId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Forbidden. You do not own the parent event of this category." });
            }

            var hasEnrolments = await _context.Enrolments.AnyAsync(e => e.CategoryID == id);
            if (hasEnrolments)
            {
                return Conflict(new { message = "Cannot delete a category that has registered participants." });
            }

            _context.Categories.Remove(cat);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}