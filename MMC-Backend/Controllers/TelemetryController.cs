using MMC_Backend.Models;
using MMC_Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace MMC_Backend.Controllers;

[ApiController]
[Route("api/telemetry")]
public class TelemetryController : ControllerBase
{
    private readonly ITelemetryService _service;

    public TelemetryController(ITelemetryService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> ReceiveTelemetry([FromBody] TelemetryIngestRequest? data, CancellationToken cancellationToken)
    {
        var validationErrors = Validate(data);
        if (validationErrors.Count > 0)
        {
            return BadRequest(new { errors = validationErrors });
        }

        var stored = await _service.StoreAsync(data!, cancellationToken);

        Console.WriteLine($"[{stored.StationId}/{stored.DeviceId}] T:{stored.TemperatureC:F1}C V:{stored.VibrationMmS:F1}mm/s L:{stored.LoadPercent}% Result:{stored.TestResult}");

        return Ok(new { status = "received" });
    }

    [HttpGet]
    public Task<IReadOnlyList<TelemetryRecord>> GetRecent([FromQuery] int limit = 100, CancellationToken cancellationToken = default)
    {
        return _service.GetRecentAsync(limit, cancellationToken);
    }

    [HttpGet("recent")]
    public Task<IReadOnlyList<TelemetryRecord>> GetRecentAlias([FromQuery] int limit = 100, CancellationToken cancellationToken = default)
    {
        return _service.GetRecentAsync(limit, cancellationToken);
    }

    [HttpGet("latest")]
    public Task<IReadOnlyList<TelemetryRecord>> GetLatest(CancellationToken cancellationToken)
    {
        return _service.GetLatestAsync(cancellationToken);
    }

    private static List<string> Validate(TelemetryIngestRequest? data)
    {
        var errors = new List<string>();

        if (data is null)
        {
            errors.Add("request body is required.");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(data.DeviceId))
        {
            errors.Add("deviceId is required.");
        }

        if (string.IsNullOrWhiteSpace(data.StationId))
        {
            errors.Add("stationId is required.");
        }

        if (data.CycleCount < 0)
        {
            errors.Add("cycleCount must be greater than or equal to 0.");
        }

        if (data.UptimeMs < 0)
        {
            errors.Add("uptimeMs must be greater than or equal to 0.");
        }

        if (data.TemperatureC < -20 || data.TemperatureC > 120)
        {
            errors.Add("temperatureC must be between -20 and 120.");
        }

        if (data.VibrationMmS < 0 || data.VibrationMmS > 100)
        {
            errors.Add("vibrationMmS must be between 0 and 100.");
        }

        if (data.LoadPercent < 0 || data.LoadPercent > 100)
        {
            errors.Add("loadPercent must be between 0 and 100.");
        }

        if (!Enum.IsDefined(data.TestResult))
        {
            errors.Add("testResult must be Running, Pass, or Fail.");
        }

        return errors;
    }
}
