using MMC_Backend.Models;
using MMC_Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace MMC_Backend.Controllers;

[ApiController]
[Route("api/alarms")]
public class AlarmsController : ControllerBase
{
    private readonly ITelemetryService _service;

    public AlarmsController(ITelemetryService service)
    {
        _service = service;
    }

    [HttpGet("active")]
    public Task<IReadOnlyList<AlarmState>> GetActive(CancellationToken cancellationToken)
    {
        return _service.GetActiveAlarmsAsync(cancellationToken);
    }
}
