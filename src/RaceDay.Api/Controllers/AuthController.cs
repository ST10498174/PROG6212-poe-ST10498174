using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthController(RaceDayDbContext db, IPasswordHasher<User> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    /// <summary>Registers a new Organiser or Participant account and stores a hashed password.</summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var roleName = request.Role.Trim();
        if (roleName != "Organiser" && roleName != "Participant")
        {
            return BadRequest(new { message = "Role must be either Organiser or Participant." });
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email))
        {
            return Conflict(new { message = "An account with this email address already exists." });
        }

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
        if (role == null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "The RaceDay roles are not configured." });
        }

        var user = new User
        {
            RoleId = role.RoleId,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            PhoneNumber = request.PhoneNumber.Trim(),
            DateOfBirth = request.DateOfBirth.Date,
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Register), new
        {
            user.UserId,
            user.FirstName,
            user.LastName,
            user.Email,
            Role = role.RoleName
        });
    }

    /// <summary>Logs a registered user in and creates a server-side session containing user ID and role.</summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            return Unauthorized(new { message = "Invalid email address or password." });
        }

        var passwordResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (passwordResult == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { message = "Invalid email address or password." });
        }

        HttpContext.Session.SetInt32("UserId", user.UserId);
        HttpContext.Session.SetString("Role", user.Role!.RoleName);

        return Ok(new
        {
            message = "Login successful.",
            user.UserId,
            user.FirstName,
            user.LastName,
            user.Email,
            Role = user.Role.RoleName
        });
    }

    /// <summary>Ends the current authenticated session.</summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Logout()
    {
        var accessError = RequireAuthenticated();
        if (accessError != null) return accessError;

        HttpContext.Session.Clear();
        return NoContent();
    }
}
