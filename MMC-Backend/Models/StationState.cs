using System.ComponentModel.DataAnnotations;

namespace MMC_Backend.Models;

public class StationState
{
    [MaxLength(64)]
    public string DeviceId { get; set; } = string.Empty;

    [MaxLength(64)]
    public string StationId { get; set; } = string.Empty;

    public MachineState CurrentState { get; set; } = MachineState.Offline;

    public long? LatestTelemetryRecordId { get; set; }

    public long LastCycleCount { get; set; }

    [MaxLength(64)]
    public string? LastAlarmCode { get; set; }

    public DateTime LastSeenUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
