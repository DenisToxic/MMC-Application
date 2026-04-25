using MMC_Backend.Models;
using MMC_Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace MMC_Backend.Controllers;

[ApiController]
[Route("api/stations")]
public class StationsController : ControllerBase
{
    private readonly ITelemetryService _service;

    public StationsController(ITelemetryService service)
    {
        _service = service;
    }

    [HttpGet("summary")]
    public Task<IReadOnlyList<StationSummary>> GetSummary(CancellationToken cancellationToken)
    {
        return _service.GetStationSummariesAsync(cancellationToken);
    }
}
