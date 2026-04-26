namespace MMC_Backend.Models;

public class TelemetryIngestRequest
{
    public string? DeviceId { get; set; }

    public string? StationId { get; set; }

    public long CycleCount { get; set; }

    public long UptimeMs { get; set; }

    public double TemperatureC { get; set; }

    public double VibrationMmS { get; set; }

    public int LoadPercent { get; set; }

    public TestResult TestResult { get; set; } = TestResult.Running;

    public bool Heartbeat { get; set; } = true;

    public bool MaintenanceMode { get; set; }

    public string? AlarmCode { get; set; }

    public string? AlarmText { get; set; }

    public DateTime? DeviceTimestampUtc { get; set; }
}
