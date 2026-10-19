using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // The mappings below mirror the Part 1 ERD and RaceDayDB.sql file.
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(r => r.RoleId);
            entity.HasIndex(r => r.RoleName).IsUnique();
            entity.HasCheckConstraint("CK_Roles_RoleName", "[RoleName] IN (N'Organiser', N'Participant')");

            entity.HasData(
                new Role { RoleId = 1, RoleName = "Organiser" },
                new Role { RoleId = 2, RoleName = "Participant" });
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.UserId);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.DateOfBirth).HasColumnType("date");
            entity.Property(u => u.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");

            entity.HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.EventId);
            entity.Property(e => e.EventDate).HasColumnType("date");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasCheckConstraint("CK_Events_Distance", "[DistanceKm] > 0");
            entity.HasCheckConstraint("CK_Events_EventType", "[EventType] IN (N'Run', N'Walk', N'Cycle')");

            entity.HasOne(e => e.Organiser)
                .WithMany(u => u.OrganisedEvents)
                .HasForeignKey(e => e.OrganiserUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.CategoryId);
            entity.HasIndex(c => new { c.EventId, c.Name }).IsUnique();
            entity.HasIndex(c => new { c.EventId, c.CategoryId }).IsUnique();
            entity.HasCheckConstraint("CK_Categories_CategoryType", "[CategoryType] IN (N'Age', N'Distance')");
            entity.HasCheckConstraint(
                "CK_Categories_Values",
                "(([CategoryType] = N'Age' AND [MinAge] IS NOT NULL AND [MaxAge] IS NOT NULL AND [MinAge] >= 0 AND [MaxAge] >= [MinAge] AND [DistanceKm] IS NULL) OR ([CategoryType] = N'Distance' AND [DistanceKm] IS NOT NULL AND [DistanceKm] > 0 AND [MinAge] IS NULL AND [MaxAge] IS NULL))");

            entity.HasOne(c => c.Event)
                .WithMany(e => e.Categories)
                .HasForeignKey(c => c.EventId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Enrolment>(entity =>
        {
            entity.HasKey(e => e.EnrolmentId);
            entity.HasIndex(e => new { e.ParticipantUserId, e.EventId }).IsUnique();
            entity.Property(e => e.EnrolmentDate).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(e => e.Status).HasDefaultValue("Pending");
            entity.HasCheckConstraint("CK_Enrolments_Status", "[Status] IN (N'Pending', N'Confirmed')");

            entity.HasOne(e => e.Participant)
                .WithMany(u => u.Enrolments)
                .HasForeignKey(e => e.ParticipantUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Event)
                .WithMany(ev => ev.Enrolments)
                .HasForeignKey(e => e.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Enrolments)
                .HasForeignKey(e => new { e.EventId, e.CategoryId })
                .HasPrincipalKey(c => new { c.EventId, c.CategoryId })
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Result>(entity =>
        {
            entity.HasKey(r => r.ResultId);
            entity.HasIndex(r => r.EnrolmentId).IsUnique();
            entity.Property(r => r.FinishTime).HasColumnType("time(0)");
            entity.Property(r => r.IsPublished).HasDefaultValue(false);
            entity.Property(r => r.RecordedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasCheckConstraint("CK_Results_FinishingPosition", "[FinishingPosition] > 0");

            entity.HasOne(r => r.Enrolment)
                .WithOne(e => e.Result)
                .HasForeignKey<Result>(r => r.EnrolmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
