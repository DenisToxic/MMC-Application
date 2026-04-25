using System.ComponentModel.DataAnnotations;

namespace MMC_Backend.Models;

public class TelemetryRecord
{
    public long Id { get; set; }

    [MaxLength(64)]
    public string DeviceId { get; set; } = string.Empty;

    [MaxLength(64)]
    public string StationId { get; set; } = string.Empty;

    public long CycleCount { get; set; }

    public long UptimeMs { get; set; }

    public double TemperatureC { get; set; }

    public double VibrationMmS { get; set; }

    public int LoadPercent { get; set; }

    public TestResult TestResult { get; set; }

    [MaxLength(64)]
    public string? AlarmCode { get; set; }

    [MaxLength(256)]
    public string? AlarmText { get; set; }

    public DateTime? DeviceTimestampUtc { get; set; }

    public DateTime ReceivedAtUtc { get; set; }
}
