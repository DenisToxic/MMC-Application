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
        var deviceId = request.DeviceId!.Trim();
        var stationId = request.StationId!.Trim();
        var alarmCode = FirstNonBlank(request.AlarmCode, thresholdAlarm?.AlarmCode);
        var alarmText = FirstNonBlank(request.AlarmText, thresholdAlarm?.AlarmText);
        var newState = EvaluateMachineState(request, alarmCode);

        var state = await _db.StationStates
            .SingleOrDefaultAsync(x => x.StationId == stationId && x.DeviceId == deviceId, cancellationToken);

        var previousState = state is null ? MachineState.Offline : ApplyOfflineWindow(state, now);
        var previousAlarmCode = state?.LastAlarmCode;
        var previousCycleCount = state?.LastCycleCount ?? 0;

        var record = new TelemetryRecord
        {
            DeviceId = deviceId,
            StationId = stationId,
            CycleCount = request.CycleCount,
            UptimeMs = request.UptimeMs,
            TemperatureC = request.TemperatureC,
            VibrationMmS = request.VibrationMmS,
            LoadPercent = request.LoadPercent,
            TestResult = request.TestResult,
            AlarmCode = alarmCode,
            AlarmText = alarmText,
            DeviceTimestampUtc = request.DeviceTimestampUtc,
            ReceivedAtUtc = now
        };

        _db.TelemetryRecords.Add(record);
        await _db.SaveChangesAsync(cancellationToken);

        if (state is null)
        {
            state = new StationState
            {
                DeviceId = deviceId,
                StationId = stationId
            };
            _db.StationStates.Add(state);
        }

        state.CurrentState = newState;
        state.LatestTelemetryRecordId = record.Id;
        state.LastCycleCount = request.CycleCount;
        state.LastAlarmCode = alarmCode;
        state.LastSeenUtc = now;
        state.UpdatedAtUtc = now;

        AddProductionEvents(record, previousState, newState, previousAlarmCode, previousCycleCount, thresholdAlarm, now);

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
        var latestRecordIds = await _db.StationStates
            .AsNoTracking()
            .Where(x => x.LatestTelemetryRecordId != null)
            .Select(x => x.LatestTelemetryRecordId!.Value)
            .ToListAsync(cancellationToken);

        return await _db.TelemetryRecords
            .AsNoTracking()
            .Where(x => latestRecordIds.Contains(x.Id))
            .OrderBy(x => x.StationId)
            .ThenBy(x => x.DeviceId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StationState>> GetCurrentStatesAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var states = await _db.StationStates
            .AsNoTracking()
            .OrderBy(x => x.StationId)
            .ThenBy(x => x.DeviceId)
            .ToListAsync(cancellationToken);

        return states
            .Select(x => new StationState
            {
                DeviceId = x.DeviceId,
                StationId = x.StationId,
                CurrentState = ApplyOfflineWindow(x, now),
                LatestTelemetryRecordId = x.LatestTelemetryRecordId,
                LastCycleCount = x.LastCycleCount,
                LastAlarmCode = x.LastAlarmCode,
                LastSeenUtc = x.LastSeenUtc,
                UpdatedAtUtc = x.UpdatedAtUtc
            })
            .ToList();
    }

    public async Task<IReadOnlyList<AlarmState>> GetActiveAlarmsAsync(CancellationToken cancellationToken)
    {
        var latest = await GetLatestAsync(cancellationToken);
        var stateByStation = (await GetCurrentStatesAsync(cancellationToken))
            .ToDictionary(x => (x.StationId, x.DeviceId));
        var now = DateTime.UtcNow;
        var alarms = new List<AlarmState>();

        foreach (var record in latest)
        {
            var state = stateByStation[(record.StationId, record.DeviceId)];
            if (state.CurrentState == MachineState.Offline)
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

        var states = (await GetCurrentStatesAsync(cancellationToken))
            .ToDictionary(x => (x.StationId, x.DeviceId));

        return records
            .GroupBy(x => new { x.StationId, x.DeviceId })
            .Select(group =>
            {
                var latest = group.First();
                var passCount = group.Count(x => x.TestResult == TestResult.Pass);
                var failCount = group.Count(x => x.TestResult == TestResult.Fail);
                var completed = passCount + failCount;
                var lastAlarm = group.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.AlarmCode));
                var state = states[(latest.StationId, latest.DeviceId)];

                return new StationSummary
                {
                    DeviceId = latest.DeviceId,
                    StationId = latest.StationId,
                    TotalCycles = group.Max(x => x.CycleCount),
                    PassCount = passCount,
                    FailCount = failCount,
                    PassRatePercent = completed == 0 ? 0 : Math.Round(passCount * 100.0 / completed, 1),
                    LastSeenUtc = state.LastSeenUtc,
                    LastAlarmCode = lastAlarm?.AlarmCode,
                    State = state.CurrentState
                };
            })
            .OrderBy(x => x.StationId)
            .ThenBy(x => x.DeviceId)
            .ToList();
    }

    public async Task<IReadOnlyList<ProductionEvent>> GetRecentEventsAsync(int limit, CancellationToken cancellationToken)
    {
        var boundedLimit = Math.Clamp(limit, 1, 500);

        return await _db.ProductionEvents
            .AsNoTracking()
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(boundedLimit)
            .ToListAsync(cancellationToken);
    }

    private void AddProductionEvents(
        TelemetryRecord record,
        MachineState previousState,
        MachineState newState,
        string? previousAlarmCode,
        long previousCycleCount,
        AlarmState? thresholdAlarm,
        DateTime now)
    {
        if (previousState != newState)
        {
            AddEvent(record, ProductionEventType.StateChanged, "STATE_CHANGED", $"State changed from {previousState} to {newState}.", now, previousState, newState);
        }

        if (previousState == MachineState.Offline && newState != MachineState.Offline)
        {
            AddEvent(record, ProductionEventType.DeviceReconnected, "DEVICE_RECONNECTED", "Telemetry stream restored.", now, previousState, newState);
        }

        if (!string.IsNullOrWhiteSpace(record.AlarmCode) && record.AlarmCode != previousAlarmCode)
        {
            AddEvent(record, ProductionEventType.AlarmRaised, record.AlarmCode, record.AlarmText ?? "Device reported an alarm.", now, previousState, newState);
        }

        if (thresholdAlarm is not null)
        {
            AddEvent(record, ProductionEventType.ThresholdViolation, thresholdAlarm.AlarmCode, thresholdAlarm.AlarmText, now, previousState, newState);
        }

        if (record.CycleCount > previousCycleCount && record.TestResult is TestResult.Pass or TestResult.Fail)
        {
            AddEvent(record, ProductionEventType.TestCompleted, $"TEST_{record.TestResult.ToString().ToUpperInvariant()}", $"Cycle {record.CycleCount} completed with result {record.TestResult}.", now, previousState, newState);
        }
    }

    private void AddEvent(
        TelemetryRecord record,
        ProductionEventType eventType,
        string eventCode,
        string message,
        DateTime occurredAtUtc,
        MachineState? previousState = null,
        MachineState? newState = null)
    {
        _db.ProductionEvents.Add(new ProductionEvent
        {
            DeviceId = record.DeviceId,
            StationId = record.StationId,
            EventType = eventType,
            EventCode = eventCode,
            Message = message,
            PreviousState = previousState,
            NewState = newState,
            TelemetryRecordId = record.Id,
            OccurredAtUtc = occurredAtUtc
        });
    }

    private MachineState ApplyOfflineWindow(StationState state, DateTime now)
    {
        return now - state.LastSeenUtc > TimeSpan.FromSeconds(_options.OfflineAfterSeconds)
            ? MachineState.Offline
            : state.CurrentState;
    }

    private MachineState EvaluateMachineState(TelemetryIngestRequest request, string? alarmCode)
    {
        if (request.MaintenanceMode)
        {
            return MachineState.Maintenance;
        }

        if (!string.IsNullOrWhiteSpace(alarmCode) || request.TestResult == TestResult.Fail)
        {
            return MachineState.Fault;
        }

        return request.Heartbeat && (request.TestResult == TestResult.Running || request.LoadPercent > 0)
            ? MachineState.Running
            : MachineState.Idle;
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
