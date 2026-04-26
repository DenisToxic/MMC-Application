param(
    [string]$BaseUrl = "http://localhost:5000",
    [string]$StationId = "TEST-CELL-01",
    [string]$DeviceId = "ESP-DEMO-01",
    [int]$Cycles = 30,
    [int]$DelaySeconds = 1
)

$ErrorActionPreference = "Stop"

function Send-Telemetry {
    param(
        [int]$Cycle,
        [double]$TemperatureC,
        [double]$VibrationMmS,
        [int]$LoadPercent,
        [string]$TestResult,
        [string]$AlarmCode = $null,
        [string]$AlarmText = $null,
        [bool]$MaintenanceMode = $false
    )

    $payload = @{
        deviceId = $DeviceId
        stationId = $StationId
        cycleCount = $Cycle
        uptimeMs = $Cycle * 5000
        temperatureC = $TemperatureC
        vibrationMmS = $VibrationMmS
        loadPercent = $LoadPercent
        testResult = $TestResult
        heartbeat = $true
        maintenanceMode = $MaintenanceMode
        alarmCode = $AlarmCode
        alarmText = $AlarmText
        deviceTimestampUtc = (Get-Date).ToUniversalTime().ToString("o")
    } | ConvertTo-Json

    Invoke-RestMethod `
        -Method Post `
        -Uri "$BaseUrl/api/telemetry" `
        -ContentType "application/json" `
        -Body $payload | Out-Null

    $state = if ($MaintenanceMode) { "Maintenance" } elseif ($AlarmCode -or $TestResult -eq "Fail") { "Fault" } elseif ($LoadPercent -eq 0) { "Idle" } else { "Running" }
    Write-Host ("[{0}] cycle={1} result={2} state={3} temp={4}C vibration={5}mm/s load={6}%" -f $StationId, $Cycle, $TestResult, $state, $TemperatureC, $VibrationMmS, $LoadPercent)
}

Write-Host "Sending demo telemetry to $BaseUrl/api/telemetry"
Write-Host "Open $BaseUrl/dashboard to watch the state, alarms, and event log."

for ($cycle = 1; $cycle -le $Cycles; $cycle++) {
    if ($cycle -eq 8) {
        Send-Telemetry -Cycle $cycle -TemperatureC 36.8 -VibrationMmS 4.2 -LoadPercent 78 -TestResult "Running" -AlarmCode "HIGH_TEMPERATURE" -AlarmText "Temperature exceeded process limit."
    }
    elseif ($cycle -eq 14) {
        Send-Telemetry -Cycle $cycle -TemperatureC 25.2 -VibrationMmS 9.4 -LoadPercent 71 -TestResult "Running" -AlarmCode "HIGH_VIBRATION" -AlarmText "Vibration exceeded process limit."
    }
    elseif ($cycle -eq 20) {
        Send-Telemetry -Cycle $cycle -TemperatureC 24.5 -VibrationMmS 2.8 -LoadPercent 64 -TestResult "Fail" -AlarmCode "TEST_FAILED" -AlarmText "End-of-line test failed."
    }
    elseif ($cycle -eq 24) {
        Send-Telemetry -Cycle $cycle -TemperatureC 23.0 -VibrationMmS 0.4 -LoadPercent 0 -TestResult "Running" -MaintenanceMode $true
    }
    elseif ($cycle % 6 -eq 0) {
        Send-Telemetry -Cycle $cycle -TemperatureC 24.0 -VibrationMmS 2.1 -LoadPercent 0 -TestResult "Pass"
    }
    else {
        $temperature = [Math]::Round(23.5 + (Get-Random -Minimum 0 -Maximum 40) / 10, 1)
        $vibration = [Math]::Round(1.0 + (Get-Random -Minimum 0 -Maximum 35) / 10, 1)
        $load = Get-Random -Minimum 35 -Maximum 82
        Send-Telemetry -Cycle $cycle -TemperatureC $temperature -VibrationMmS $vibration -LoadPercent $load -TestResult "Running"
    }

    Start-Sleep -Seconds $DelaySeconds
}

Write-Host "Done. Query current state: $BaseUrl/api/telemetry/states"
Write-Host "Query event history:  $BaseUrl/api/telemetry/events"
