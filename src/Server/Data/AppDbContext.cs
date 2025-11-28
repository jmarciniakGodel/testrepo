using Microsoft.EntityFrameworkCore;
using Server.Models;

namespace Server.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Attendant> Attendants { get; set; }
    public DbSet<Meeting> Meetings { get; set; }
    public DbSet<MeetingAttendance> MeetingAttendances { get; set; }
    public DbSet<Summary> Summaries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Attendant>()
            .HasIndex(a => a.Email)
            .IsUnique();

        modelBuilder.Entity<MeetingAttendance>()
            .HasOne(ma => ma.Meeting)
            .WithMany(m => m.MeetingAttendances)
            .HasForeignKey(ma => ma.MeetingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MeetingAttendance>()
            .HasOne(ma => ma.Attendant)
            .WithMany(a => a.MeetingAttendances)
            .HasForeignKey(ma => ma.AttendantId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Meeting>()
            .HasOne(m => m.Summary)
            .WithMany(s => s.Meetings)
            .HasForeignKey(m => m.SummaryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
