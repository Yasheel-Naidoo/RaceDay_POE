using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    [Route("api")]
    public class CategoriesController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public CategoriesController(RaceDayDbContext context)
        {
            _context = context;
        }

        [HttpGet("events/{eventId:int}/categories")]
        public async Task<IActionResult> GetCategoriesForEvent(int eventId)
        {
            var eventExists = await _context.Events.AnyAsync(e => e.EventID == eventId);
            if (!eventExists) return NotFound(new { message = "Event not found." });

            var categories = await _context.Categories
                .Where(c => c.EventID == eventId)
                .Select(c => new CategoryResponseDto(
                    c.CategoryID,
                    c.EventID,
                    c.CategoryName,
                    c.DistanceKm,
                    c.EntryFee,
                    c.MaxParticipants,
                    c.Enrolments.Count(en => en.Status != "Withdrawn")
                ))
                .ToListAsync();

            return Ok(categories);
        }

        [HttpPost("events/{eventId:int}/categories")]
        public async Task<IActionResult> CreateCategory(int eventId, [FromBody] CreateCategoryDto dto)
        {
            var role = HttpContext.Session.GetString("Role");
            var userId = HttpContext.Session.GetInt32("UserId");

            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null) return NotFound(new { message = "Event not found." });

            if (role != "Organiser" || ev.OrganiserID != userId)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Forbidden. Only the event owner can add categories." });

            var categoryName = dto.CategoryName?.Trim() ?? string.Empty;
            if (categoryName.Length == 0)
                return BadRequest(new { message = "A category name is required." });

            var nameTaken = await _context.Categories
                .AnyAsync(c => c.EventID == eventId && c.CategoryName.ToLower() == categoryName.ToLower());

            if (nameTaken)
                return Conflict(new { message = $"A category named '{categoryName}' already exists for this event." });

            var category = new Category
            {
                EventID = eventId,
                CategoryName = categoryName,
                DistanceKm = dto.DistanceKm,
                EntryFee = dto.EntryFee,
                MaxParticipants = dto.MaxParticipants
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            var responseDto = new CategoryResponseDto(
                category.CategoryID,
                category.EventID,
                category.CategoryName,
                category.DistanceKm,
                category.EntryFee,
                category.MaxParticipants,
                0
            );

            return StatusCode(StatusCodes.Status201Created, responseDto);
        }

        [HttpPut("categories/{id:int}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryDto dto)
        {
            var role = HttpContext.Session.GetString("Role");
            var userId = HttpContext.Session.GetInt32("UserId");

            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null) return NotFound(new { message = "Category not found." });

            if (role != "Organiser" || category.Event.OrganiserID != userId)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Forbidden. Only the event owner can update categories." });

            var categoryName = dto.CategoryName?.Trim() ?? string.Empty;
            if (categoryName.Length == 0)
                return BadRequest(new { message = "A category name is required." });

            var nameTaken = await _context.Categories
                .AnyAsync(c => c.EventID == category.EventID && c.CategoryID != id &&
                               c.CategoryName.ToLower() == categoryName.ToLower());

            if (nameTaken)
                return Conflict(new { message = $"A category named '{categoryName}' already exists for this event." });

            category.CategoryName = categoryName;
            category.DistanceKm = dto.DistanceKm;
            category.EntryFee = dto.EntryFee;
            category.MaxParticipants = dto.MaxParticipants;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Category updated successfully." });
        }

        [HttpDelete("categories/{id:int}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var role = HttpContext.Session.GetString("Role");
            var userId = HttpContext.Session.GetInt32("UserId");

            var category = await _context.Categories
                .Include(c => c.Event)
                .Include(c => c.Enrolments)
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null) return NotFound(new { message = "Category not found." });

            if (role != "Organiser" || category.Event.OrganiserID != userId)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Forbidden. Only the event owner can delete categories." });

            if (category.Enrolments.Any())
                return Conflict(new { message = "This category already has enrolments, so it cannot be deleted." });

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Category deleted successfully." });
        }
    }
}