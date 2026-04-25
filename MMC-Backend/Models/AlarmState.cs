namespace MMC_Backend.Models;

public class AlarmState
{
    public string DeviceId { get; set; } = string.Empty;

    public string StationId { get; set; } = string.Empty;

    public string AlarmCode { get; set; } = string.Empty;

    public string AlarmText { get; set; } = string.Empty;

    public string Severity { get; set; } = "Warning";

    public DateTime DetectedAtUtc { get; set; }
}
