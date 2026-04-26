namespace MMC_Backend.Models;

public class StationSummary
{
    public string DeviceId { get; set; } = string.Empty;

    public string StationId { get; set; } = string.Empty;

    public long TotalCycles { get; set; }

    public int PassCount { get; set; }

    public int FailCount { get; set; }

    public double PassRatePercent { get; set; }

    public DateTime? LastSeenUtc { get; set; }

    public string? LastAlarmCode { get; set; }

    public MachineState State { get; set; } = MachineState.Offline;

    public string Status => State.ToString();
}
