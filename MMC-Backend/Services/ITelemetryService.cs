using MMC_Backend.Models;

namespace MMC_Backend.Services;

public interface ITelemetryService
{
    Task<TelemetryRecord> StoreAsync(TelemetryIngestRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<TelemetryRecord>> GetRecentAsync(int limit, CancellationToken cancellationToken);

    Task<IReadOnlyList<TelemetryRecord>> GetLatestAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<AlarmState>> GetActiveAlarmsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<StationSummary>> GetStationSummariesAsync(CancellationToken cancellationToken);
}
