using MMC_Backend.Models;
using MMC_Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace MMC_Backend.Controllers;

public class DashboardController : Controller
{
    private readonly ITelemetryService _service;

    public DashboardController(ITelemetryService service)
    {
        _service = service;
    }

    [HttpGet("")]
    [HttpGet("dashboard")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = new DashboardViewModel
        {
            RecentTelemetry = await _service.GetRecentAsync(50, cancellationToken),
            LatestTelemetry = await _service.GetLatestAsync(cancellationToken),
            ActiveAlarms = await _service.GetActiveAlarmsAsync(cancellationToken),
            StationSummaries = await _service.GetStationSummariesAsync(cancellationToken),
            RecentEvents = await _service.GetRecentEventsAsync(20, cancellationToken),
            RenderedAtUtc = DateTime.UtcNow
        };

        return View(model);
    }
}
