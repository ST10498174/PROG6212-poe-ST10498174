using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[Route("api")]
public class CategoriesController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public CategoriesController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>Returns all categories available for a specific event. This endpoint is public.</summary>
    [HttpGet("events/{eventId:int}/categories")]
    public async Task<IActionResult> GetCategories(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
        {
            return NotFound(new { message = "Event was not found." });
        }

        var categories = await _db.Categories
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.CategoryId)
            .Select(c => new
            {
                c.CategoryId,
                c.EventId,
                c.Name,
                c.CategoryType,
                c.MinAge,
                c.MaxAge,
                c.DistanceKm
            })
            .ToListAsync();

        return Ok(categories);
    }

    /// <summary>Adds an age or distance category to an event owned by the logged-in Organiser.</summary>
    [HttpPost("events/{eventId:int}/categories")]
    public async Task<IActionResult> CreateCategory(int eventId, CategoryRequest request)
    {
        var accessError = RequireRole("Organiser");
        if (accessError != null) return accessError;

        var eventItem = await _db.Events.FindAsync(eventId);
        if (eventItem == null) return NotFound(new { message = "Event was not found." });
        if (eventItem.OrganiserUserId != CurrentUserId) return StatusCode(403, new { message = "You may only manage categories for your own events." });

        var validationError = ValidateCategory(request);
        if (validationError != null) return validationError;

        if (await _db.Categories.AnyAsync(c => c.EventId == eventId && c.Name == request.Name.Trim()))
        {
            return Conflict(new { message = "A category with this name already exists for the event." });
        }

        var category = new Category
        {
            EventId = eventId,
            Name = request.Name.Trim(),
            CategoryType = NormalizeCategoryType(request.CategoryType),
            MinAge = request.MinAge,
            MaxAge = request.MaxAge,
            DistanceKm = request.DistanceKm
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return Created($"/api/events/{eventId}/categories", category);
    }

    /// <summary>Updates an existing category belonging to the logged-in Organiser's event.</summary>
    [HttpPut("categories/{id:int}")]
    public async Task<IActionResult> UpdateCategory(int id, CategoryRequest request)
    {
        var accessError = RequireRole("Organiser");
        if (accessError != null) return accessError;

        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category == null) return NotFound(new { message = "Category was not found." });
        if (category.Event!.OrganiserUserId != CurrentUserId) return StatusCode(403, new { message = "You may only manage categories for your own events." });

        var validationError = ValidateCategory(request);
        if (validationError != null) return validationError;

        var duplicate = await _db.Categories.AnyAsync(c => c.EventId == category.EventId && c.Name == request.Name.Trim() && c.CategoryId != id);
        if (duplicate) return Conflict(new { message = "A category with this name already exists for the event." });

        category.Name = request.Name.Trim();
        category.CategoryType = NormalizeCategoryType(request.CategoryType);
        category.MinAge = request.MinAge;
        category.MaxAge = request.MaxAge;
        category.DistanceKm = request.DistanceKm;

        await _db.SaveChangesAsync();
        return Ok(category);
    }

    /// <summary>Deletes a category when it has no dependent enrolments.</summary>
    [HttpDelete("categories/{id:int}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var accessError = RequireRole("Organiser");
        if (accessError != null) return accessError;

        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category == null) return NotFound(new { message = "Category was not found." });
        if (category.Event!.OrganiserUserId != CurrentUserId) return StatusCode(403, new { message = "You may only manage categories for your own events." });

        if (await _db.Enrolments.AnyAsync(e => e.CategoryId == id))
        {
            return Conflict(new { message = "This category cannot be deleted because an enrolment uses it." });
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private IActionResult? ValidateCategory(CategoryRequest request)
    {
        var type = NormalizeCategoryType(request.CategoryType);

        if (type == "Age")
        {
            if (!request.MinAge.HasValue || !request.MaxAge.HasValue || request.MinAge < 0 || request.MaxAge < request.MinAge || request.DistanceKm.HasValue)
            {
                return BadRequest(new { message = "Age categories require valid MinAge and MaxAge values and must not include DistanceKm." });
            }
        }
        else if (type == "Distance")
        {
            if (!request.DistanceKm.HasValue || request.DistanceKm <= 0 || request.MinAge.HasValue || request.MaxAge.HasValue)
            {
                return BadRequest(new { message = "Distance categories require a positive DistanceKm and must not include age values." });
            }
        }
        else
        {
            return BadRequest(new { message = "Category type must be Age or Distance." });
        }

        return null;
    }

    private static string NormalizeCategoryType(string categoryType)
    {
        var value = categoryType.Trim();
        if (value.Length == 0) return value;
        return char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();
    }
}
