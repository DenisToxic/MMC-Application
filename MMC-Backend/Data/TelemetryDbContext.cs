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

    public DbSet<StationState> StationStates => Set<StationState>();

    public DbSet<ProductionEvent> ProductionEvents => Set<ProductionEvent>();

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

        var stationState = modelBuilder.Entity<StationState>();

        stationState.ToTable("station_states");
        stationState.HasKey(x => new { x.StationId, x.DeviceId });
        stationState.Property(x => x.DeviceId).IsRequired().HasMaxLength(64);
        stationState.Property(x => x.StationId).IsRequired().HasMaxLength(64);
        stationState.Property(x => x.CurrentState).HasConversion<string>().HasMaxLength(32);
        stationState.Property(x => x.LastAlarmCode).HasMaxLength(64);
        stationState.HasIndex(x => x.LastSeenUtc);

        var productionEvent = modelBuilder.Entity<ProductionEvent>();

        productionEvent.ToTable("production_events");
        productionEvent.HasKey(x => x.Id);
        productionEvent.Property(x => x.DeviceId).IsRequired().HasMaxLength(64);
        productionEvent.Property(x => x.StationId).IsRequired().HasMaxLength(64);
        productionEvent.Property(x => x.EventType).HasConversion<string>().HasMaxLength(32);
        productionEvent.Property(x => x.EventCode).HasMaxLength(64);
        productionEvent.Property(x => x.Message).IsRequired().HasMaxLength(256);
        productionEvent.Property(x => x.PreviousState).HasConversion<string>().HasMaxLength(32);
        productionEvent.Property(x => x.NewState).HasConversion<string>().HasMaxLength(32);
        productionEvent.Property(x => x.OccurredAtUtc).IsRequired();
        productionEvent.HasIndex(x => x.OccurredAtUtc);
        productionEvent.HasIndex(x => new { x.StationId, x.DeviceId, x.OccurredAtUtc });
    }
}
