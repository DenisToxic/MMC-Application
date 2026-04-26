using System.ComponentModel.DataAnnotations;

namespace MMC_Backend.Models;

public class ProductionEvent
{
    public long Id { get; set; }

    [MaxLength(64)]
    public string DeviceId { get; set; } = string.Empty;

    [MaxLength(64)]
    public string StationId { get; set; } = string.Empty;

    public ProductionEventType EventType { get; set; }

    [MaxLength(64)]
    public string? EventCode { get; set; }

    [MaxLength(256)]
    public string Message { get; set; } = string.Empty;

    public MachineState? PreviousState { get; set; }

    public MachineState? NewState { get; set; }

    public long? TelemetryRecordId { get; set; }

    public DateTime OccurredAtUtc { get; set; }
}
