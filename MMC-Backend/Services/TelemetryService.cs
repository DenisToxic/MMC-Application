using MMC_Backend.Data;
using MMC_Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MMC_Backend.Services;

public class TelemetryService : ITelemetryService
{
    private readonly TelemetryDbContext _db;
    private readonly TelemetryOptions _options;

    public TelemetryService(TelemetryDbContext db, IOptions<TelemetryOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public async Task<TelemetryRecord> StoreAsync(TelemetryIngestRequest request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var thresholdAlarm = EvaluateThresholdAlarm(request, now);

        var record = new TelemetryRecord
        {
            DeviceId = request.DeviceId!.Trim(),
            StationId = request.StationId!.Trim(),
            CycleCount = request.CycleCount,
            UptimeMs = request.UptimeMs,
            TemperatureC = request.TemperatureC,
            VibrationMmS = request.VibrationMmS,
            LoadPercent = request.LoadPercent,
            TestResult = request.TestResult,
            AlarmCode = FirstNonBlank(request.AlarmCode, thresholdAlarm?.AlarmCode),
            AlarmText = FirstNonBlank(request.AlarmText, thresholdAlarm?.AlarmText),
            DeviceTimestampUtc = request.DeviceTimestampUtc,
            ReceivedAtUtc = now
        };

        _db.TelemetryRecords.Add(record);
        await _db.SaveChangesAsync(cancellationToken);

        return record;
    }

    public async Task<IReadOnlyList<TelemetryRecord>> GetRecentAsync(int limit, CancellationToken cancellationToken)
    {
        var boundedLimit = Math.Clamp(limit, 1, 500);

        return await _db.TelemetryRecords
            .AsNoTracking()
            .OrderByDescending(x => x.ReceivedAtUtc)
            .Take(boundedLimit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TelemetryRecord>> GetLatestAsync(CancellationToken cancellationToken)
    {
        var records = await _db.TelemetryRecords
            .AsNoTracking()
            .OrderByDescending(x => x.ReceivedAtUtc)
            .ToListAsync(cancellationToken);

        return records
            .GroupBy(x => new { x.StationId, x.DeviceId })
            .Select(x => x.First())
            .OrderBy(x => x.StationId)
            .ThenBy(x => x.DeviceId)
            .ToList();
    }

    public async Task<IReadOnlyList<AlarmState>> GetActiveAlarmsAsync(CancellationToken cancellationToken)
    {
        var latest = await GetLatestAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var alarms = new List<AlarmState>();

        foreach (var record in latest)
        {
            if (now - record.ReceivedAtUtc > TimeSpan.FromSeconds(_options.OfflineAfterSeconds))
            {
                alarms.Add(new AlarmState
                {
                    DeviceId = record.DeviceId,
                    StationId = record.StationId,
                    AlarmCode = "DEVICE_OFFLINE",
                    AlarmText = $"No telemetry received for more than {_options.OfflineAfterSeconds} seconds.",
                    Severity = "Critical",
                    DetectedAtUtc = now
                });

                continue;
            }

            alarms.AddRange(EvaluateThresholdAlarms(record, now));
        }

        return alarms
            .OrderByDescending(x => x.Severity == "Critical")
            .ThenBy(x => x.StationId)
            .ThenBy(x => x.DeviceId)
            .ToList();
    }

    public async Task<IReadOnlyList<StationSummary>> GetStationSummariesAsync(CancellationToken cancellationToken)
    {
        var records = await _db.TelemetryRecords
            .AsNoTracking()
            .OrderByDescending(x => x.ReceivedAtUtc)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;

        return records
            .GroupBy(x => new { x.StationId, x.DeviceId })
            .Select(group =>
            {
                var latest = group.First();
                var passCount = group.Count(x => x.TestResult == TestResult.Pass);
                var failCount = group.Count(x => x.TestResult == TestResult.Fail);
                var completed = passCount + failCount;
                var lastAlarm = group.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.AlarmCode));

                return new StationSummary
                {
                    DeviceId = latest.DeviceId,
                    StationId = latest.StationId,
                    TotalCycles = group.Max(x => x.CycleCount),
                    PassCount = passCount,
                    FailCount = failCount,
                    PassRatePercent = completed == 0 ? 0 : Math.Round(passCount * 100.0 / completed, 1),
                    LastSeenUtc = latest.ReceivedAtUtc,
                    LastAlarmCode = lastAlarm?.AlarmCode,
                    Status = now - latest.ReceivedAtUtc > TimeSpan.FromSeconds(_options.OfflineAfterSeconds)
                        ? "Offline"
                        : EvaluateThresholdAlarms(latest, now).Any() ? "Alarm" : "Running"
                };
            })
            .OrderBy(x => x.StationId)
            .ThenBy(x => x.DeviceId)
            .ToList();
    }

    private AlarmState? EvaluateThresholdAlarm(TelemetryIngestRequest request, DateTime now)
    {
        if (request.TemperatureC > _options.MaxTemperatureC)
        {
            return CreateAlarm(request.DeviceId!, request.StationId!, "HIGH_TEMPERATURE", $"Temperature {request.TemperatureC:F1} C exceeds {_options.MaxTemperatureC:F1} C.", now);
        }

        if (request.VibrationMmS > _options.MaxVibrationMmS)
        {
            return CreateAlarm(request.DeviceId!, request.StationId!, "HIGH_VIBRATION", $"Vibration {request.VibrationMmS:F1} mm/s exceeds {_options.MaxVibrationMmS:F1} mm/s.", now);
        }

        if (request.LoadPercent > _options.MaxLoadPercent)
        {
            return CreateAlarm(request.DeviceId!, request.StationId!, "HIGH_LOAD", $"Load {request.LoadPercent}% exceeds {_options.MaxLoadPercent}%.", now);
        }

        return null;
    }

    private IReadOnlyList<AlarmState> EvaluateThresholdAlarms(TelemetryRecord record, DateTime now)
    {
        var alarms = new List<AlarmState>();

        if (record.TemperatureC > _options.MaxTemperatureC)
        {
            alarms.Add(CreateAlarm(record.DeviceId, record.StationId, "HIGH_TEMPERATURE", $"Temperature {record.TemperatureC:F1} C exceeds {_options.MaxTemperatureC:F1} C.", now));
        }

        if (record.VibrationMmS > _options.MaxVibrationMmS)
        {
            alarms.Add(CreateAlarm(record.DeviceId, record.StationId, "HIGH_VIBRATION", $"Vibration {record.VibrationMmS:F1} mm/s exceeds {_options.MaxVibrationMmS:F1} mm/s.", now));
        }

        if (record.LoadPercent > _options.MaxLoadPercent)
        {
            alarms.Add(CreateAlarm(record.DeviceId, record.StationId, "HIGH_LOAD", $"Load {record.LoadPercent}% exceeds {_options.MaxLoadPercent}%.", now));
        }

        if (!string.IsNullOrWhiteSpace(record.AlarmCode))
        {
            alarms.Add(CreateAlarm(record.DeviceId, record.StationId, record.AlarmCode, record.AlarmText ?? "Device reported an alarm.", record.ReceivedAtUtc));
        }

        return alarms
            .GroupBy(x => x.AlarmCode)
            .Select(x => x.First())
            .ToList();
    }

    private static AlarmState CreateAlarm(string deviceId, string stationId, string alarmCode, string alarmText, DateTime detectedAtUtc)
    {
        return new AlarmState
        {
            DeviceId = deviceId,
            StationId = stationId,
            AlarmCode = alarmCode,
            AlarmText = alarmText,
            Severity = "Warning",
            DetectedAtUtc = detectedAtUtc
        };
    }

    private static string? FirstNonBlank(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
    }
}
