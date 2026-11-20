using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

/// <summary>
/// Loads realistic South African demonstration data so both role interfaces can be shown in the video.
/// Seeding is switched on with the "SeedDemoData" setting and only runs when the Users table is empty,
/// so it never overwrites real data. The application does not depend on this data: with seeding switched
/// off every page works against an empty database.
/// Event dates are relative to the day of seeding so there are always past events (with results)
/// and upcoming events (open for entries).
/// </summary>
public static class DemoDataSeeder
{
    /// <summary>Password shared by every demo account (documented in the README).</summary>
    public const string DemoPassword = "RaceDay123!";

    public static async Task SeedAsync(RaceDayDbContext db, IPasswordHasher<User> hasher, ILogger logger)
    {
        if (await db.Users.AnyAsync())
        {
            logger.LogInformation("Demo data skipped because users already exist.");
            return;
        }

        var organiserRoleId = (await db.Roles.SingleAsync(r => r.RoleName == "Organiser")).RoleId;
        var participantRoleId = (await db.Roles.SingleAsync(r => r.RoleName == "Participant")).RoleId;
        var today = DateTime.Today;

        // ---- Users ---------------------------------------------------------------------------
        User NewUser(int roleId, string first, string last, string email, string phone, DateTime dob)
        {
            var user = new User
            {
                RoleId = roleId,
                FirstName = first,
                LastName = last,
                Email = email,
                PhoneNumber = phone,
                DateOfBirth = dob,
                CreatedAt = DateTime.UtcNow
            };
            user.PasswordHash = hasher.HashPassword(user, DemoPassword);
            return user;
        }

        var thandi = NewUser(organiserRoleId, "Thandi", "Nkosi", "thandi.organiser@raceday.co.za", "0825550101", new DateTime(1988, 3, 14));
        var pieter = NewUser(organiserRoleId, "Pieter", "van der Merwe", "pieter.organiser@raceday.co.za", "0825550102", new DateTime(1979, 11, 2));

        var participants = new List<User>
        {
            NewUser(participantRoleId, "Lerato", "Khumalo", "lerato.participant@raceday.co.za", "0825550201", new DateTime(2001, 9, 20)),
            NewUser(participantRoleId, "Sipho", "Dlamini", "sipho.participant@raceday.co.za", "0825550202", new DateTime(1995, 6, 8)),
            NewUser(participantRoleId, "Ayesha", "Patel", "ayesha.patel@example.co.za", "0825550203", new DateTime(1992, 1, 30)),
            NewUser(participantRoleId, "Johan", "Botha", "johan.botha@example.co.za", "0825550204", new DateTime(1984, 7, 17)),
            NewUser(participantRoleId, "Nomvula", "Mthembu", "nomvula.mthembu@example.co.za", "0825550205", new DateTime(1999, 12, 5)),
            NewUser(participantRoleId, "Kyle", "Naidoo", "kyle.naidoo@example.co.za", "0825550206", new DateTime(2006, 4, 11)),
            NewUser(participantRoleId, "Zanele", "Zulu", "zanele.zulu@example.co.za", "0825550207", new DateTime(1990, 8, 23)),
            NewUser(participantRoleId, "Ruan", "Steyn", "ruan.steyn@example.co.za", "0825550208", new DateTime(1976, 2, 9)),
            NewUser(participantRoleId, "Fatima", "Adams", "fatima.adams@example.co.za", "0825550209", new DateTime(1997, 10, 1)),
            NewUser(participantRoleId, "Themba", "Ngcobo", "themba.ngcobo@example.co.za", "0825550210", new DateTime(1987, 5, 27)),
            NewUser(participantRoleId, "Megan", "Pillay", "megan.pillay@example.co.za", "0825550211", new DateTime(2003, 3, 3)),
            NewUser(participantRoleId, "Bongani", "Shabalala", "bongani.shabalala@example.co.za", "0825550212", new DateTime(1982, 9, 15))
        };

        db.Users.AddRange(thandi, pieter);
        db.Users.AddRange(participants);
        await db.SaveChangesAsync();

        // ---- Events ----------------------------------------------------------------------------
        Event NewEvent(User organiser, string name, string description, int daysFromToday, string location, decimal distance, string type) => new()
        {
            OrganiserUserId = organiser.UserId,
            Name = name,
            Description = description,
            EventDate = today.AddDays(daysFromToday),
            Location = location,
            DistanceKm = distance,
            EventType = type,
            CreatedAt = DateTime.UtcNow
        };

        var umhlanga = NewEvent(thandi, "Umhlanga Sunrise 10K",
            "A flat and fast out-and-back along the Umhlanga promenade, starting at first light beside the lighthouse. Water tables every 2.5 km and a beachfront finish.",
            -21, "Umhlanga Promenade, Durban", 10m, "Run");
        var goldenMile = NewEvent(thandi, "Golden Mile Beachfront Walk",
            "A relaxed family walk along Durban's Golden Mile from uShaka to the Moses Mabhida pedestrian bridge. Prams and leashed dogs welcome.",
            -7, "Golden Mile, Durban", 5m, "Walk");
        var midlands = NewEvent(thandi, "Midlands Meander Cycle Classic",
            "Rolling country roads through the KZN Midlands with two distance options. Fully marshalled route, mechanical support and a farm-stall finish village.",
            18, "Howick, KwaZulu-Natal", 65m, "Cycle");
        var stellenbosch = NewEvent(pieter, "Stellenbosch Winelands 15K",
            "A scenic road race between the vineyards of the Stellenbosch valley. Moderate climbs in the first half are rewarded with a fast downhill finish.",
            -30, "Stellenbosch, Western Cape", 15m, "Run");
        var soweto = NewEvent(pieter, "Soweto Heritage 21K",
            "A half marathon past Vilakazi Street, the Hector Pieterson Memorial and Orlando Towers, finishing inside Orlando Stadium.",
            35, "Orlando Stadium, Soweto", 21.1m, "Run");
        var kirstenbosch = NewEvent(pieter, "Kirstenbosch Sunset Walk",
            "An evening walk on the lower slopes of Table Mountain through Kirstenbosch's contour paths, ending with a picnic on the lawns.",
            10, "Kirstenbosch, Cape Town", 8m, "Walk");
        var peninsula = NewEvent(pieter, "Cape Peninsula Coastal Ride",
            "The classic loop around the Cape Peninsula via Chapman's Peak and Smitswinkel Bay. Seeded start groups and timing mats at every 20 km.",
            60, "Green Point, Cape Town", 109m, "Cycle");

        db.Events.AddRange(umhlanga, goldenMile, midlands, stellenbosch, soweto, kirstenbosch, peninsula);
        await db.SaveChangesAsync();

        // ---- Categories ------------------------------------------------------------------------
        Category Distance(Event e, string name, decimal km) => new() { EventId = e.EventId, Name = name, CategoryType = "Distance", DistanceKm = km };
        Category Age(Event e, string name, short min, short max) => new() { EventId = e.EventId, Name = name, CategoryType = "Age", MinAge = min, MaxAge = max };

        var umhlangaOpen = Distance(umhlanga, "10km Open", 10m);
        var umhlangaJunior = Age(umhlanga, "Under 20", 0, 19);
        var umhlangaVeteran = Age(umhlanga, "Veterans 40+", 40, 120);
        var goldenMile5 = Distance(goldenMile, "5km Walk", 5m);
        var midlands65 = Distance(midlands, "65km Classic", 65m);
        var midlands35 = Distance(midlands, "35km Fun Ride", 35m);
        var stellenbosch15 = Distance(stellenbosch, "15km Open", 15m);
        var stellenboschSenior = Age(stellenbosch, "Senior 20-39", 20, 39);
        var soweto21 = Distance(soweto, "21.1km Half Marathon", 21.1m);
        var soweto10 = Distance(soweto, "10km", 10m);
        var sowetoJunior = Age(soweto, "Under 20", 0, 19);
        var kirstenbosch8 = Distance(kirstenbosch, "8km Walk", 8m);
        var peninsula109 = Distance(peninsula, "109km Classic", 109m);

        db.Categories.AddRange(umhlangaOpen, umhlangaJunior, umhlangaVeteran, goldenMile5, midlands65, midlands35,
            stellenbosch15, stellenboschSenior, soweto21, soweto10, sowetoJunior, kirstenbosch8, peninsula109);
        await db.SaveChangesAsync();

        // ---- Enrolments ------------------------------------------------------------------------
        var enrolments = new List<Enrolment>();
        Enrolment Enrol(User p, Event e, Category c, string status, int daysBeforeEvent)
        {
            var enrolment = new Enrolment
            {
                ParticipantUserId = p.UserId,
                EventId = e.EventId,
                CategoryId = c.CategoryId,
                Status = status,
                EnrolmentDate = e.EventDate.AddDays(-daysBeforeEvent) < DateTime.UtcNow
                    ? e.EventDate.AddDays(-daysBeforeEvent)
                    : DateTime.UtcNow
            };
            enrolments.Add(enrolment);
            return enrolment;
        }

        // Past events: every participant who entered was confirmed and has a result.
        var umhlangaEntries = participants.Select((p, i) =>
            Enrol(p, umhlanga, i == 5 ? umhlangaJunior : (i == 3 || i == 7 || i == 11) ? umhlangaVeteran : umhlangaOpen, "Confirmed", 30 + i)).ToList();
        var goldenMileEntries = participants.Take(8).Select((p, i) => Enrol(p, goldenMile, goldenMile5, "Confirmed", 14 + i)).ToList();
        var stellenboschEntries = participants.Where((_, i) => i != 4 && i != 10)
            .Select((p, i) => Enrol(p, stellenbosch, i % 3 == 0 ? stellenboschSenior : stellenbosch15, "Confirmed", 40 + i)).ToList();

        // Upcoming events: a mix of Pending and Confirmed entries for the Organiser to manage.
        Enrol(participants[0], midlands, midlands35, "Pending", 3);
        Enrol(participants[1], midlands, midlands65, "Confirmed", 10);
        Enrol(participants[3], midlands, midlands65, "Confirmed", 12);
        Enrol(participants[6], midlands, midlands35, "Pending", 2);
        Enrol(participants[9], midlands, midlands65, "Pending", 1);
        Enrol(participants[0], soweto, soweto21, "Confirmed", 20);
        Enrol(participants[2], soweto, soweto10, "Pending", 6);
        Enrol(participants[5], soweto, sowetoJunior, "Pending", 4);
        Enrol(participants[8], soweto, soweto21, "Confirmed", 15);
        Enrol(participants[11], soweto, soweto10, "Pending", 2);
        Enrol(participants[1], peninsula, peninsula109, "Pending", 5);
        Enrol(participants[7], peninsula, peninsula109, "Confirmed", 9);

        db.Enrolments.AddRange(enrolments);
        await db.SaveChangesAsync();

        // ---- Results ---------------------------------------------------------------------------
        // Finish times are spread realistically for each distance; positions follow finish-time order.
        var results = new List<Result>();
        // (i * 7 + offset) % count shuffles finishing order differently per event (7 shares no factor with 8, 10 or 12 entrants).
        void AddResults(List<Enrolment> entries, int fastestSeconds, int spreadSeconds, int offset, Func<int, bool> isPublished)
        {
            var timed = entries
                .Select((e, i) => new { Enrolment = e, Seconds = fastestSeconds + ((i * 7 + offset) % entries.Count) * spreadSeconds / Math.Max(1, entries.Count - 1) + i * 13 })
                .OrderBy(x => x.Seconds)
                .ToList();

            for (var position = 1; position <= timed.Count; position++)
            {
                results.Add(new Result
                {
                    EnrolmentId = timed[position - 1].Enrolment.EnrolmentId,
                    FinishTime = TimeSpan.FromSeconds(timed[position - 1].Seconds),
                    FinishingPosition = position,
                    IsPublished = isPublished(position),
                    RecordedAt = DateTime.UtcNow
                });
            }
        }

        AddResults(umhlangaEntries, 36 * 60 + 12, 38 * 60, 6, _ => true);          // 10 km: 36 min to ~74 min
        AddResults(stellenboschEntries, 58 * 60 + 40, 55 * 60, 2, _ => true);      // 15 km: just under 1 h to ~2 h
        AddResults(goldenMileEntries, 41 * 60 + 5, 25 * 60, 3, p => p <= 6);       // 5 km walk: two results still in draft

        db.Results.AddRange(results);
        await db.SaveChangesAsync();

        logger.LogInformation("Seeded RaceDay demo data: {Users} users, {Events} events, {Enrolments} enrolments, {Results} results.",
            participants.Count + 2, 7, enrolments.Count, results.Count);
    }
}
