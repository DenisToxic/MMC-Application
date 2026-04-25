namespace MMC_Backend.Models;

public class DashboardViewModel
{
    public IReadOnlyList<TelemetryRecord> RecentTelemetry { get; set; } = [];

    public IReadOnlyList<TelemetryRecord> LatestTelemetry { get; set; } = [];

    public IReadOnlyList<AlarmState> ActiveAlarms { get; set; } = [];

    public IReadOnlyList<StationSummary> StationSummaries { get; set; } = [];

    public DateTime RenderedAtUtc { get; set; }
}
