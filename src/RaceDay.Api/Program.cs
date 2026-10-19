using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Adds controllers used by the REST API.
builder.Services.AddControllers();

// Connects Entity Framework Core to the SQL Server database described in appsettings.json.
builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RaceDayConnection")));

// Session management stores the logged-in user's ID and role on the server.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RaceDay.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

// ASP.NET Core's password hasher is used so plain-text passwords are never stored.
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// Swagger documents every controller endpoint and allows the API to be tested in a browser.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "RESTful API for RaceDay event management. Authentication uses server-side sessions."
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseSession();
app.MapControllers();

// Code-First model creation is used when the configured database has not yet been created.
// If the Part 1 SQL script has already created RaceDayDB, EF Core simply uses that matching schema.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
    db.Database.EnsureCreated();
}

app.Run();

public partial class Program { }
