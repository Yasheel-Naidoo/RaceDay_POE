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
    public class AuthController : ControllerBase
    {
        private readonly RaceDayDbContext _context;
        private readonly IPasswordService _passwordService;

        public AuthController(RaceDayDbContext context, IPasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
        {
            var emailExists = await _context.Organisers.AnyAsync(o => o.Email == dto.Email) ||
                              await _context.Participants.AnyAsync(p => p.Email == dto.Email);

            if (emailExists)
                return Conflict(new { message = "An account with this email already exists." });

            var passwordHash = _passwordService.HashPassword(dto.Password);

            if (dto.Role.Equals("Organiser", StringComparison.OrdinalIgnoreCase))
            {
                var organiser = new Organiser
                {
                    FullName = dto.FullName,
                    Email = dto.Email,
                    Phone = dto.Phone,
                    OrganisationName = dto.OrganisationName ?? "Independent",
                    PasswordHash = passwordHash,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Organisers.Add(organiser);
                await _context.SaveChangesAsync();
                return StatusCode(StatusCodes.Status201Created, new { userId = organiser.OrganiserID, role = "Organiser" });
            }
            else
            {
                var participant = new Participant
                {
                    FullName = dto.FullName,
                    Email = dto.Email,
                    Phone = dto.Phone,
                    DateOfBirth = dto.DateOfBirth,
                    PasswordHash = passwordHash,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Participants.Add(participant);
                await _context.SaveChangesAsync();
                return StatusCode(StatusCodes.Status201Created, new { userId = participant.ParticipantID, role = "Participant" });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            var organiser = await _context.Organisers.FirstOrDefaultAsync(o => o.Email == dto.Email);
            if (organiser != null && _passwordService.VerifyPassword(dto.Password, organiser.PasswordHash))
            {
                HttpContext.Session.SetInt32("UserId", organiser.OrganiserID);
                HttpContext.Session.SetString("Role", "Organiser");
                return Ok(new { userId = organiser.OrganiserID, fullName = organiser.FullName, role = "Organiser" });
            }

            var participant = await _context.Participants.FirstOrDefaultAsync(p => p.Email == dto.Email);
            if (participant != null && _passwordService.VerifyPassword(dto.Password, participant.PasswordHash))
            {
                HttpContext.Session.SetInt32("UserId", participant.ParticipantID);
                HttpContext.Session.SetString("Role", "Participant");
                return Ok(new { userId = participant.ParticipantID, fullName = participant.FullName, role = "Participant" });
            }

            return Unauthorized(new { message = "Invalid email or password." });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Ok(new { message = "Logged out successfully." });
        }
    }
}