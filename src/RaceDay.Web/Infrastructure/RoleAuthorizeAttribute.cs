using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Web.Infrastructure;

/// <summary>
/// Server-side access control for MVC controllers and actions, based on the role stored in the session.
///
/// [RoleAuthorize] with no roles: any logged-in user.
/// [RoleAuthorize(Roles.Organiser)]: Organisers only.
///
/// Unauthenticated users are redirected to the login page (with a return URL), and users with the
/// wrong role are redirected to the access-denied page. Navigation links are also hidden per role,
/// but this filter is what actually enforces the rules, so typing a URL directly does not bypass them.
/// The API enforces the same rules again on every request.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class RoleAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _roles;

    public RoleAuthorizeAttribute(params string[] roles)
    {
        _roles = roles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var httpContext = context.HttpContext;

        // Protected pages must not be served from the browser cache after logout (e.g. via the Back button).
        httpContext.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
        httpContext.Response.Headers.Pragma = "no-cache";

        var user = httpContext.GetCurrentUser();
        if (user == null)
        {
            var returnUrl = httpContext.Request.Path + httpContext.Request.QueryString;
            context.Result = new RedirectToActionResult("Login", "Account", new { area = "", returnUrl });
            return;
        }

        if (_roles.Length > 0 && !_roles.Contains(user.Role, StringComparer.OrdinalIgnoreCase))
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", new { area = "" });
        }
    }
}
