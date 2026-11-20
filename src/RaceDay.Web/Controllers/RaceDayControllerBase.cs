using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>
/// Shared helpers for every RaceDay MVC controller: access to the API client and logged-in user,
/// one-line success/error messages, and mapping of API validation errors onto form fields.
/// </summary>
public abstract class RaceDayControllerBase : Controller
{
    protected RaceDayControllerBase(IRaceDayApiClient api)
    {
        Api = api;
    }

    protected IRaceDayApiClient Api { get; }

    /// <summary>The logged-in user. Only call from actions protected by <see cref="RoleAuthorizeAttribute"/>.</summary>
    protected SessionUser CurrentUser => HttpContext.GetCurrentUser()
        ?? throw new InvalidOperationException("No user is logged in.");

    protected void Success(string message) => TempData[AlertKeys.Success] = message;
    protected void Error(string message) => TempData[AlertKeys.Error] = message;
    protected void Info(string message) => TempData[AlertKeys.Info] = message;

    /// <summary>
    /// Copies API validation errors onto the matching form fields (e.g. "Email") and adds the
    /// overall message to the form summary, so users see the API's feedback beside their inputs.
    /// </summary>
    protected void AddApiErrors(ApiResult result, IReadOnlyDictionary<string, string>? fieldMap = null)
    {
        foreach (var (field, messages) in result.FieldErrors)
        {
            var key = fieldMap != null && fieldMap.TryGetValue(field, out var mapped) ? mapped : field;
            foreach (var message in messages)
            {
                ModelState.AddModelError(key, message);
            }
        }

        if (!string.IsNullOrWhiteSpace(result.ErrorMessage) && result.FieldErrors.Count == 0)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
        }
    }

    /// <summary>Sends a page that could not load its data to the friendly not-found / error page.</summary>
    protected IActionResult ApiFailure(ApiResult result, string? backUrl = null)
    {
        if (result.IsForbidden)
        {
            return RedirectToAction("AccessDenied", "Account", new { area = "" });
        }

        Response.StatusCode = result.IsNotFound ? StatusCodes.Status404NotFound : StatusCodes.Status503ServiceUnavailable;
        ViewData["Title"] = result.IsNotFound ? "Not found" : "Something went wrong";
        ViewData["Message"] = result.ErrorMessage;
        ViewData["BackUrl"] = backUrl;
        return View("~/Views/Shared/Problem.cshtml");
    }
}
