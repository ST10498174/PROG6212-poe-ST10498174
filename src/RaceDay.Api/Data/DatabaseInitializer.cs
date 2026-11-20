using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

/// <summary>
/// Creates the database on start-up and (optionally) loads demonstration data.
/// In Docker the SQL Server container can take 20-40 seconds to accept connections,
/// so creation is retried instead of letting the API crash on its first attempt.
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitialiseAsync(IServiceProvider services, IConfiguration configuration, ILogger logger)
    {
        var maxAttempts = configuration.GetValue("Database:StartupRetryCount", 12);
        var delay = TimeSpan.FromSeconds(configuration.GetValue("Database:StartupRetryDelaySeconds", 5));

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var scope = services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();

                // Code-First creation: builds the schema from the EF model when RaceDayDB does not exist yet.
                // If the Part 1 SQL script already created the database, EnsureCreated leaves it untouched.
                await db.Database.EnsureCreatedAsync();

                if (configuration.GetValue("SeedDemoData", false))
                {
                    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
                    await DemoDataSeeder.SeedAsync(db, hasher, logger);
                }

                logger.LogInformation("RaceDay database is ready.");
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning("Database not ready (attempt {Attempt}/{Max}): {Message}. Retrying in {Delay}s.",
                    attempt, maxAttempts, ex.Message, delay.TotalSeconds);
                await Task.Delay(delay);
            }
        }
    }
}
