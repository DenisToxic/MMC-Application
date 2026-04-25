namespace MMC_Backend.Services;

public class TelemetryOptions
{
    public double MaxTemperatureC { get; set; } = 32.0;

    public double MaxVibrationMmS { get; set; } = 7.5;

    public int MaxLoadPercent { get; set; } = 85;

    public int OfflineAfterSeconds { get; set; } = 20;
}
