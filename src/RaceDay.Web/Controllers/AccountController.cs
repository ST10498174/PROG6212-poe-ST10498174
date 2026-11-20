using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Registration, login and logout. All credential checks are performed by the API.</summary>
public class AccountController : RaceDayControllerBase
{
    public AccountController(IRaceDayApiClient api) : base(api)
    {
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        var user = HttpContext.GetCurrentUser();
        if (user != null)
        {
            return Redirect(user.DashboardUrl());
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Start from a clean session so nothing from a previous user carries over.
        HttpContext.Session.Clear();

        var result = await Api.LoginAsync(model.Email, model.Password);
        if (!result.Success || result.Data == null)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Invalid email address or password.");
            model.Password = string.Empty;
            return View(model);
        }

        // The user's ID and role are written to the server-side session; the browser only holds the session cookie.
        HttpContext.Session.SignIn(result.Data);

        // Load the profile picture for the navigation bar (non-critical, so failures are ignored).
        var profile = await Api.GetProfileAsync();
        if (profile.Success && profile.Data != null)
        {
            HttpContext.Session.UpdateDisplayDetails(profile.Data);
        }

        Success($"Welcome back, {result.Data.FirstName}.");

        // Only local return URLs are followed, preventing open-redirect attacks.
        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return Redirect(HttpContext.GetCurrentUser()!.DashboardUrl());
    }

    [HttpGet]
    public IActionResult Register(string? role = null)
    {
        if (HttpContext.GetCurrentUser() is { } user)
        {
            return Redirect(user.DashboardUrl());
        }

        return View(new RegisterViewModel { Role = role == Roles.Organiser ? Roles.Organiser : Roles.Participant });
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (model.DateOfBirth.HasValue && model.DateOfBirth.Value.Date > DateTime.Today)
        {
            ModelState.AddModelError(nameof(model.DateOfBirth), "Date of birth can't be in the future.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await Api.RegisterAsync(new RegisterRequest
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.Email,
            Password = model.Password,
            PhoneNumber = model.PhoneNumber,
            DateOfBirth = model.DateOfBirth!.Value.Date,
            Role = model.Role
        });

        if (!result.Success)
        {
            AddApiErrors(result);
            return View(model);
        }

        // Log the new user straight in so they land on their dashboard.
        HttpContext.Session.Clear();
        var login = await Api.LoginAsync(model.Email, model.Password);
        if (login.Success && login.Data != null)
        {
            HttpContext.Session.SignIn(login.Data);
            Success($"Your {model.Role.ToLowerInvariant()} account is ready. Welcome to RaceDay, {login.Data.FirstName}.");
            return Redirect(HttpContext.GetCurrentUser()!.DashboardUrl());
        }

        Success("Your account has been created. Log in to continue.");
        return RedirectToAction(nameof(Login));
    }

    /// <summary>Ends the API session and clears the MVC session, then returns to the landing page.</summary>
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await Api.LogoutAsync();
        HttpContext.Session.Clear();
        Response.Cookies.Delete(".RaceDay.Web.Session");
        Success("You've been logged out.");
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View(HttpContext.GetCurrentUser());
    }
}
