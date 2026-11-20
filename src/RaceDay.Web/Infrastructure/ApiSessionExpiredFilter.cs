using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using RaceDay.Web.Services;

namespace RaceDay.Web.Infrastructure;

/// <summary>
/// Global exception filter: when the API reports that the user's API session has expired,
/// the MVC session is cleared and the user is sent to the login page with an explanation,
/// instead of seeing an error page. The original page is kept as the return URL.
/// </summary>
public class ApiSessionExpiredFilter : IExceptionFilter
{
    private readonly ITempDataDictionaryFactory _tempDataFactory;

    public ApiSessionExpiredFilter(ITempDataDictionaryFactory tempDataFactory)
    {
        _tempDataFactory = tempDataFactory;
    }

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not ApiSessionExpiredException)
        {
            return;
        }

        var httpContext = context.HttpContext;
        httpContext.Session.Clear();

        var tempData = _tempDataFactory.GetTempData(httpContext);
        tempData[AlertKeys.Error] = "Your session has expired. Please log in again.";

        var returnUrl = HttpMethods.IsGet(httpContext.Request.Method)
            ? httpContext.Request.Path + httpContext.Request.QueryString
            : null;

        context.Result = new RedirectToActionResult("Login", "Account", new { area = "", returnUrl });
        context.ExceptionHandled = true;
    }
}

/// <summary>TempData keys used by the shared alert partial.</summary>
public static class AlertKeys
{
    public const string Success = "AlertSuccess";
    public const string Error = "AlertError";
    public const string Info = "AlertInfo";
}
