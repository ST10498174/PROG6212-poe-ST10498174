using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC with two global filters: anti-forgery validation on every form POST, and
// friendly handling of an expired API session.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    options.Filters.Add<ApiSessionExpiredFilter>();
});

// Server-side session holds the logged-in user's ID, role and the relayed API session cookie.
// The 30-minute idle timeout matches the API's own session timeout.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RaceDay.Web.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

builder.Services.AddHttpContextAccessor();

// Typed HttpClient for the Part 2 API. Cookies are handled manually (see RaceDayApiClient) so that
// one shared handler never mixes up different users' API sessions.
builder.Services.Configure<RaceDayApiOptions>(builder.Configuration.GetSection(RaceDayApiOptions.SectionName));
builder.Services.AddHttpClient<IRaceDayApiClient, RaceDayApiClient>((serviceProvider, client) =>
    {
        var options = serviceProvider.GetRequiredService<IOptions<RaceDayApiOptions>>().Value;
        client.BaseAddress = new Uri(options.BaseUrl.EndsWith('/') ? options.BaseUrl : options.BaseUrl + "/");
        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        UseCookies = false,
        AllowAutoRedirect = false
    });

var app = builder.Build();

// One fixed culture (British English: day-month-year dates, "." decimal separator) so the app
// formats and parses values identically on Windows, macOS and inside the Linux container.
var culture = new CultureInfo("en-GB");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culture),
    SupportedCultures = new[] { culture },
    SupportedUICultures = new[] { culture }
});

if (!app.Environment.IsDevelopment())
{
    // Unhandled errors show a friendly page instead of a stack trace.
    app.UseExceptionHandler("/Home/Error");
}

// 404s and other bare status codes are rendered with the RaceDay error page.
app.UseStatusCodePagesWithReExecute("/Home/StatusCode", "?code={0}");

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

// Organiser and Participant interfaces live in their own MVC areas.
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
