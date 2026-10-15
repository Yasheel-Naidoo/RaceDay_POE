using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Filters;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [RequireAuth]
    public class UsersController : ControllerBase
    {
        private readonly RaceDayDbContext _context;
        public UsersController(RaceDayDbContext context) => _context = context;

        /// <summary>
        /// Returns the logged-in user's profile details.
        /// </summary>
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = HttpContext.Session.GetInt32("UserId")!.Value;
            var role = HttpContext.Session.GetString("Role")!;

            if (role == "Organiser")
            {
                var o = await _context.Organisers.FindAsync(userId);
                if (o == null) return NotFound();
                return Ok(new UserProfileDto(o.OrganiserID, o.FullName, o.Email, role, o.Phone, o.OrganisationName, null, null, null));
            }

            var p = await _context.Participants.FindAsync(userId);
            if (p == null) return NotFound();
            return Ok(new UserProfileDto(p.ParticipantID, p.FullName, p.Email, role, p.Phone, null, p.DateOfBirth, p.EmergencyContactName, p.EmergencyContactPhone));
        }

        /// <summary>
        /// Updates the logged-in user's profile details.
        /// </summary>
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = HttpContext.Session.GetInt32("UserId")!.Value;
            var role = HttpContext.Session.GetString("Role")!;

            if (role == "Organiser")
            {
                var o = await _context.Organisers.FindAsync(userId);
                if (o == null) return NotFound();
                if (dto.FullName != null) o.FullName = dto.FullName;
                if (dto.Phone != null) o.Phone = dto.Phone;
                await _context.SaveChangesAsync();
                return Ok(o);
            }

            var p = await _context.Participants.FindAsync(userId);
            if (p == null) return NotFound();
            if (dto.FullName != null) p.FullName = dto.FullName;
            if (dto.Phone != null) p.Phone = dto.Phone;
            if (dto.EmergencyContactName != null) p.EmergencyContactName = dto.EmergencyContactName;
            if (dto.EmergencyContactPhone != null) p.EmergencyContactPhone = dto.EmergencyContactPhone;

            await _context.SaveChangesAsync();
            return Ok(p);
        }
    }
}
