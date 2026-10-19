using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected int? CurrentUserId => HttpContext.Session.GetInt32("UserId");
    protected string? CurrentRole => HttpContext.Session.GetString("Role");

    protected IActionResult? RequireAuthenticated()
    {
        return CurrentUserId.HasValue ? null : Unauthorized(new { message = "You must log in first." });
    }

    protected IActionResult? RequireRole(string requiredRole)
    {
        if (!CurrentUserId.HasValue)
        {
            return Unauthorized(new { message = "You must log in first." });
        }

        if (!string.Equals(CurrentRole, requiredRole, StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = $"Only {requiredRole}s may use this endpoint." });
        }

        return null;
    }
}
