# Industrial Test Cell Monitor

Portfolio prototype for an industrial production test cell. An ESP8266 sends live telemetry to an ASP.NET Core backend, the backend validates the device input, derives machine state, stores trace history in PostgreSQL, and exposes an operator dashboard plus REST APIs.

This is intentionally framed as a small MES-style monitoring system, not a generic IoT sensor demo.

![Operator dashboard showing station state, alarms, station summary, and telemetry history](docs/screenshots/Dashboard.png)

![Production event history showing state changes and industrial events](docs/screenshots/Events.png)

## What It Demonstrates

- Real embedded device integration with ESP8266 over HTTP/JSON.
- ASP.NET Core ingestion API with validation and domain rules.
- PostgreSQL persistence with EF Core migrations.
- Append-only telemetry history for production traceability.
- Machine state model: `Offline`, `Idle`, `Running`, `Fault`, `Maintenance`.
- Production event log for state changes, alarms, reconnects, threshold violations, and completed tests.
- Latest-state cache so dashboard/API reads do not need to scan the full telemetry history.

## System Architecture

```text
ESP8266 Test Cell Node
     |
     | HTTP JSON telemetry
     v
ASP.NET Core Ingestion API
     |
     +--> Input validation
     +--> Threshold checks
     +--> Machine state derivation
     +--> Production event generation
     |
     v
PostgreSQL
     |
     +--> telemetry_records    append-only telemetry history
     +--> station_states       latest state cache
     +--> production_events    audit/event history
     |
     v
Operator Dashboard + REST API
```

## Industrial Semantics

The ESP8266 is treated as an untrusted data source. The backend is the validation authority: it accepts raw device telemetry, checks process thresholds, derives the authoritative machine state, writes append-only history, and maintains a latest-state cache for fast operator views.

Telemetry answers: "What did the device report?"

Station state answers: "What is the current authoritative state of the station?"

Production events answer: "What important production or system event happened?"

## Repository Layout

```text
MMC-Backend/          ASP.NET Core API, dashboard, EF Core model, migrations
arduino/main/         ESP8266 telemetry sender
scripts/             local demo tools
docs/screenshots/    portfolio screenshots
```

## Backend Stack

- .NET 8
- ASP.NET Core MVC/API
- Entity Framework Core
- PostgreSQL
- Swagger/OpenAPI

## Data Model

### `telemetry_records`

Append-only stream of raw telemetry and traceability values:

- station ID
- device ID
- cycle count
- uptime
- temperature
- vibration
- load
- test result
- alarm code/text
- device timestamp
- backend receive timestamp

### `station_states`

Latest-state cache per station/device:

- current machine state
- latest telemetry record ID
- last cycle count
- last alarm code
- last seen timestamp

### `production_events`

Audit-style event history:

- state changes
- alarm raised
- threshold violations
- test completed
- device reconnects

## API Examples

### Ingest Telemetry

`POST /api/telemetry`

```json
{
  "deviceId": "ESP-001",
  "stationId": "TEST-CELL-01",
  "cycleCount": 42,
  "uptimeMs": 12345,
  "temperatureC": 24.8,
  "vibrationMmS": 1.2,
  "loadPercent": 47,
  "testResult": "Pass",
  "heartbeat": true,
  "maintenanceMode": false,
  "alarmCode": null,
  "alarmText": null
}
```

### Read APIs

```text
GET /api/telemetry/recent
GET /api/telemetry/latest
GET /api/telemetry/states
GET /api/telemetry/events
GET /api/alarms/active
GET /api/stations/summary
GET /api/stations/states
```

## Run Locally

### 1. Configure PostgreSQL

Set the connection string with user secrets or an environment variable.

```powershell
cd MMC-Backend
dotnet user-secrets set "ConnectionStrings:TelemetryDatabase" "Host=localhost;Port=5432;Database=industrial_test_cell_monitor;Username=postgres;Password=YOUR_PASSWORD"
```

### 2. Apply Migrations

```powershell
dotnet tool restore
dotnet ef database update
```

### 3. Start the Backend

```powershell
dotnet run --launch-profile http
```

Open:

```text
http://localhost:5000/dashboard
http://localhost:5000/swagger
```

## Demo Without ESP8266

Run the demo telemetry sender from the repository root:

```powershell
.\scripts\send-demo-telemetry.ps1 -BaseUrl "http://localhost:5000"
```

The script sends normal running telemetry, threshold violations, failed tests, and maintenance samples so the dashboard shows realistic state transitions and event history.

Useful variants:

```powershell
.\scripts\send-demo-telemetry.ps1 -Cycles 60 -DelaySeconds 2
.\scripts\send-demo-telemetry.ps1 -StationId "TEST-CELL-02" -DeviceId "ESP-DEMO-02"
```

## ESP8266 Setup

1. Copy `arduino/main/secrets.example.h` to `arduino/main/secrets.h`.
2. Set Wi-Fi credentials.
3. Set `SERVER_URL` to your backend IP, for example:

```cpp
#define SERVER_URL "http://YOUR_BACKEND_IP:5000/api/telemetry"
```

4. Flash `arduino/main/main.ino` to the ESP8266.

The backend launch profile listens on `http://0.0.0.0:5000`, so another device on the same network can post telemetry to your machine.

Do not commit `arduino/main/secrets.h`. It is intentionally ignored by Git because it contains local Wi-Fi credentials.

## Portfolio Demo Flow

1. Start PostgreSQL and the backend.
2. Open the dashboard.
3. Run `scripts/send-demo-telemetry.ps1`.
4. Show the dashboard changing from running to fault/maintenance.
5. Open `/api/telemetry/events` to show the production event history.
6. Flash the ESP8266 and show real hardware sending the same payload shape.

## Screenshot Checklist

Current screenshots in `docs/screenshots/`:

- `Dashboard.png`
- `Events.png`

Useful additional screenshots:

- `Swagger.png`
- `EspSerialMonitor.png`

These screenshots are the first thing a reviewer should see after the architecture overview.

## Why This Is Industrially Relevant

Industrial monitoring systems are not just sensor dashboards. They need traceability, state semantics, validation boundaries, event history, and efficient current-state reads. This project implements those core ideas in a compact stack that maps well to .NET-based manufacturing and automation software.
