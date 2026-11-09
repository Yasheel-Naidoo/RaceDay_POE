using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    [Route("api")]
    public class UsersController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public UsersController(RaceDayDbContext context)
        {
            _context = context;
        }

        // ... your controller methods (GetProfile, UpdateProfile, etc.) ...

        [HttpPut("users/{id:int}/profile")]
        public async Task<IActionResult> UpdateProfile(int id, [FromForm] UpdateProfileDto dto, IFormFile? profilePicture)
        {
            var user = await _context.Participants.FindAsync(id);
            if (user == null) return NotFound(new { message = "User not found." });

            user.FullName = dto.FullName;
            user.Phone = dto.Phone;
            user.DateOfBirth = dto.DateOfBirth;
            user.EmergencyContactName = dto.EmergencyContactName;
            user.EmergencyContactPhone = dto.EmergencyContactPhone;

            if (profilePicture != null)
            {
                var fileService = HttpContext.RequestServices.GetRequiredService<ILocalFileService>();
                user.ProfilePictureUrl = await fileService.SaveFileAsync(profilePicture, "profiles");
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Profile updated successfully.", profilePictureUrl = user.ProfilePictureUrl });
        }
    } // Closes UsersController class
} // Closes RaceDay.Api.Controllers namespace