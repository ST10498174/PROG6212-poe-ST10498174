using Azure.Core;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using System.Reflection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Adds controllers used by the REST API.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Safety net: never fail a response because an EF entity graph contains back-references.
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

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

// Part 3: Azure Blob Storage (Azurite during development and in Docker) for event banners and profile pictures.
builder.Services.Configure<BlobStorageOptions>(builder.Configuration.GetSection(BlobStorageOptions.SectionName));
builder.Services.AddSingleton(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<IOptions<BlobStorageOptions>>().Value;
    var clientOptions = new BlobClientOptions();

    // Fail fast when the storage emulator is not running instead of retrying for a long time.
    clientOptions.Retry.MaxRetries = 2;
    clientOptions.Retry.Mode = RetryMode.Fixed;
    clientOptions.Retry.Delay = TimeSpan.FromMilliseconds(500);
    clientOptions.Retry.NetworkTimeout = TimeSpan.FromSeconds(15);

    return new BlobServiceClient(options.ConnectionString, clientOptions);
});
builder.Services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();

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

// Swagger is always on in Development and can be switched on elsewhere (for example in Docker) with Swagger:Enabled.
if (app.Environment.IsDevelopment() || app.Configuration.GetValue("Swagger:Enabled", false))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// The MVC front end calls the API server-to-server over plain HTTP (inside the Docker network or on localhost),
// so HTTPS redirection is optional and controlled by configuration rather than always forced.
if (app.Configuration.GetValue("UseHttpsRedirection", false))
{
    app.UseHttpsRedirection();
}

app.UseSession();
app.MapControllers();

// Code-First model creation is used when the configured database has not yet been created.
// If the Part 1 SQL script has already created RaceDayDB, EF Core simply uses that matching schema.
// The initializer retries while SQL Server is still starting (important in Docker) and seeds demo data when enabled.
await DatabaseInitializer.InitialiseAsync(app.Services, app.Configuration, app.Logger);

app.Run();

public partial class Program { }
