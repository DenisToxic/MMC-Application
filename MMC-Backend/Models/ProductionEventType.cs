namespace MMC_Backend.Models;

public enum ProductionEventType
{
    StateChanged = 0,
    AlarmRaised = 1,
    DeviceReconnected = 2,
    ThresholdViolation = 3,
    TestCompleted = 4
}
