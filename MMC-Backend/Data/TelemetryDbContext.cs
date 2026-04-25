using MMC_Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace MMC_Backend.Data;

public class TelemetryDbContext : DbContext
{
    public TelemetryDbContext(DbContextOptions<TelemetryDbContext> options)
        : base(options)
    {
    }

    public DbSet<TelemetryRecord> TelemetryRecords => Set<TelemetryRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var telemetry = modelBuilder.Entity<TelemetryRecord>();

        telemetry.ToTable("telemetry_records");
        telemetry.HasKey(x => x.Id);
        telemetry.Property(x => x.TestResult).HasConversion<string>().HasMaxLength(16);
        telemetry.Property(x => x.DeviceId).IsRequired().HasMaxLength(64);
        telemetry.Property(x => x.StationId).IsRequired().HasMaxLength(64);
        telemetry.Property(x => x.AlarmCode).HasMaxLength(64);
        telemetry.Property(x => x.AlarmText).HasMaxLength(256);
        telemetry.Property(x => x.ReceivedAtUtc).IsRequired();
        telemetry.HasIndex(x => x.ReceivedAtUtc);
        telemetry.HasIndex(x => new { x.StationId, x.DeviceId, x.ReceivedAtUtc });
    }
}
