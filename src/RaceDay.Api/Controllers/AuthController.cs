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

        /// <summary>
        /// Registers a new Organiser or Participant account.
        /// </summary>
        /// <response code="201">Account successfully created.</response>
        /// <response code="400">Invalid registration fields.</response>
        /// <response code="409">Email address already exists.</response>
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { message = "Email and password are required." });

            var emailExists = await _context.Organisers.AnyAsync(o => o.Email == dto.Email) ||
                              await _context.Participants.AnyAsync(p => p.Email == dto.Email);
            if (emailExists)
                return Conflict(new { message = "An account with this email already exists." });

            var hashedPassword = _passwordService.HashPassword(dto.Password);

            if (dto.Role.Equals("Organiser", StringComparison.OrdinalIgnoreCase))
            {
                var organiser = new Organiser
                {
                    FullName = dto.FullName,
                    Email = dto.Email,
                    OrganisationName = dto.OrganisationName ?? dto.FullName,
                    Phone = dto.Phone,
                    PasswordHash = hashedPassword
                };
                _context.Organisers.Add(organiser);
                await _context.SaveChangesAsync();

                HttpContext.Session.SetInt32("UserId", organiser.OrganiserID);
                HttpContext.Session.SetString("Role", "Organiser");

                return StatusCode(StatusCodes.Status201Created, new AuthResponseDto(organiser.OrganiserID, organiser.FullName, organiser.Email, "Organiser", "Registration successful."));
            }
            else if (dto.Role.Equals("Participant", StringComparison.OrdinalIgnoreCase))
            {
                var participant = new Participant
                {
                    FullName = dto.FullName,
                    Email = dto.Email,
                    Phone = dto.Phone,
                    DateOfBirth = dto.DateOfBirth,
                    PasswordHash = hashedPassword
                };
                _context.Participants.Add(participant);
                await _context.SaveChangesAsync();

                HttpContext.Session.SetInt32("UserId", participant.ParticipantID);
                HttpContext.Session.SetString("Role", "Participant");

                return StatusCode(StatusCodes.Status201Created, new AuthResponseDto(participant.ParticipantID, participant.FullName, participant.Email, "Participant", "Registration successful."));
            }

            return BadRequest(new { message = "Invalid role specified. Use 'Organiser' or 'Participant'." });
        }

        /// <summary>
        /// Authenticates credentials and starts a server-side session.
        /// </summary>
        /// <response code="200">Login successful; session initialized.</response>
        /// <response code="401">Invalid credentials.</response>
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            var organiser = await _context.Organisers.FirstOrDefaultAsync(o => o.Email == dto.Email);
            if (organiser != null && _passwordService.VerifyPassword(organiser.PasswordHash, dto.Password))
            {
                HttpContext.Session.SetInt32("UserId", organiser.OrganiserID);
                HttpContext.Session.SetString("Role", "Organiser");
                return Ok(new AuthResponseDto(organiser.OrganiserID, organiser.FullName, organiser.Email, "Organiser", "Login successful."));
            }

            var participant = await _context.Participants.FirstOrDefaultAsync(p => p.Email == dto.Email);
            if (participant != null && _passwordService.VerifyPassword(participant.PasswordHash, dto.Password))
            {
                HttpContext.Session.SetInt32("UserId", participant.ParticipantID);
                HttpContext.Session.SetString("Role", "Participant");
                return Ok(new AuthResponseDto(participant.ParticipantID, participant.FullName, participant.Email, "Participant", "Login successful."));
            }

            return Unauthorized(new { message = "Invalid email or password." });
        }

        /// <summary>
        /// Destroys current user session.
        /// </summary>
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Ok(new { message = "Logged out successfully." });
        }
    }
}