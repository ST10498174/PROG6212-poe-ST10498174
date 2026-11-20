using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Areas.Organiser.Controllers;

/// <summary>Add, edit and remove the age or distance categories of one of the Organiser's events.</summary>
public class CategoriesController : OrganiserControllerBase
{
    public CategoriesController(IRaceDayApiClient api) : base(api)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Index(int eventId)
    {
        var (eventItem, failure) = await LoadOwnedEventAsync(eventId);
        if (failure != null) return failure;

        return View(await BuildPageAsync(eventItem!, new CategoryFormViewModel { EventId = eventId }));
    }

    [HttpPost]
    public async Task<IActionResult> Create(int eventId, [Bind(Prefix = "NewCategory")] CategoryFormViewModel form)
    {
        var (eventItem, failure) = await LoadOwnedEventAsync(eventId);
        if (failure != null) return failure;

        form.EventId = eventId;
        ValidateTypeFields(form, "NewCategory.");
        if (!ModelState.IsValid)
        {
            return View("Index", await BuildPageAsync(eventItem!, form));
        }

        var result = await Api.CreateCategoryAsync(eventId, form.ToApiRequest());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "The category couldn't be added.");
            return View("Index", await BuildPageAsync(eventItem!, form));
        }

        Success($"Category \"{form.Name}\" added.");
        return RedirectToAction(nameof(Index), new { eventId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, int eventId)
    {
        var (eventItem, failure) = await LoadOwnedEventAsync(eventId);
        if (failure != null) return failure;

        var categories = await Api.GetCategoriesAsync(eventId);
        var category = categories.Data?.FirstOrDefault(c => c.CategoryId == id);
        if (category == null)
        {
            Error("That category no longer exists.");
            return RedirectToAction(nameof(Index), new { eventId });
        }

        ViewData["ManagedEvent"] = new ManagedEventViewModel { Event = eventItem!, ActiveTab = "Categories" };
        return View(new CategoryFormViewModel
        {
            CategoryId = category.CategoryId,
            EventId = eventId,
            Name = category.Name,
            CategoryType = category.CategoryType,
            MinAge = category.MinAge,
            MaxAge = category.MaxAge,
            DistanceKm = category.DistanceKm
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, CategoryFormViewModel form)
    {
        var (eventItem, failure) = await LoadOwnedEventAsync(form.EventId);
        if (failure != null) return failure;

        form.CategoryId = id;
        ViewData["ManagedEvent"] = new ManagedEventViewModel { Event = eventItem!, ActiveTab = "Categories" };
        ValidateTypeFields(form, string.Empty);
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var result = await Api.UpdateCategoryAsync(id, form.ToApiRequest());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "The category couldn't be saved.");
            return View(form);
        }

        Success($"Category \"{form.Name}\" saved.");
        return RedirectToAction(nameof(Index), new { eventId = form.EventId });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, int eventId)
    {
        var (_, failure) = await LoadOwnedEventAsync(eventId);
        if (failure != null) return failure;

        var result = await Api.DeleteCategoryAsync(id);
        if (result.Success)
        {
            Success("Category removed.");
        }
        else
        {
            // The API refuses (409) when participants have entered this category.
            Error(result.ErrorMessage ?? "The category couldn't be removed.");
        }

        return RedirectToAction(nameof(Index), new { eventId });
    }

    private async Task<CategoriesPageViewModel> BuildPageAsync(Models.Api.EventDto eventItem, CategoryFormViewModel form)
    {
        var categories = await Api.GetCategoriesAsync(eventItem.EventId);
        if (!categories.Success)
        {
            Error(categories.ErrorMessage ?? "Categories couldn't be loaded.");
        }

        return new CategoriesPageViewModel
        {
            Event = eventItem,
            ActiveTab = "Categories",
            Categories = categories.Data ?? new(),
            NewCategory = form
        };
    }

    /// <summary>Age categories need an age range; distance categories need a distance (mirrors the API rule for instant feedback).</summary>
    private void ValidateTypeFields(CategoryFormViewModel form, string prefix)
    {
        if (form.CategoryType == "Age")
        {
            if (!form.MinAge.HasValue) ModelState.AddModelError(prefix + nameof(form.MinAge), "Enter the youngest age.");
            if (!form.MaxAge.HasValue) ModelState.AddModelError(prefix + nameof(form.MaxAge), "Enter the oldest age.");
            if (form.MinAge.HasValue && form.MaxAge.HasValue && form.MaxAge < form.MinAge)
            {
                ModelState.AddModelError(prefix + nameof(form.MaxAge), "The oldest age must be at least the youngest age.");
            }
        }
        else if (form.CategoryType == "Distance" && !form.DistanceKm.HasValue)
        {
            ModelState.AddModelError(prefix + nameof(form.DistanceKm), "Enter the category distance.");
        }
    }
}
