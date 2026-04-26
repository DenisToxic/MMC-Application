# 🏭 Industrial Test Cell Monitoring System

A self-hosted **industrial telemetry and monitoring platform** designed to simulate and visualize a production test cell environment.

This project demonstrates **embedded device integration, .NET backend development, and industrial-style data processing**, inspired by real-world automation and measurement systems.

---

## ⚙️ Overview

This system represents a simplified **test cell monitoring setup**:

* 📟 Embedded device (ESP8266) simulates a machine
* 🌐 ASP.NET Core backend ingests and processes telemetry
* 🐘 PostgreSQL stores historical data
* 🖥 Web dashboard visualizes system state in real-time

The goal is to replicate core concepts found in:

* industrial automation systems
* MES (Manufacturing Execution Systems)
* machine monitoring & test environments

---

## 🧱 System Architecture

```text
ESP8266 (Machine Simulation)
        ↓ HTTP (JSON Telemetry)
ASP.NET Core Backend (.NET 8)
        ↓
PostgreSQL (Telemetry Storage)
        ↓
Operator Dashboard (Razor UI)
```

---

## 🧩 Features

### 📡 Embedded Telemetry Source

* ESP8266 sends periodic machine data
* Simulated industrial values:

  * temperature (°C)
  * vibration (mm/s)
  * load (%)
* Includes uptime and cycle tracking

---

### 🧠 Backend Processing (.NET)

* REST API for telemetry ingestion
* Validation of incoming data (untrusted device input)
* Structured domain model for industrial data
* Service-based architecture
* EF Core with PostgreSQL persistence

---

### 📊 Industrial Data Model

Each telemetry record includes:

* `deviceId` – unique machine identifier
* `stationId` – test station reference
* `cycleCount` – production/test cycles
* `uptimeMs` – device runtime
* `temperatureC`, `vibrationMmS`, `loadPercent`
* `testResult` – Pass / Fail / Running
* `alarmCode`, `alarmText`
* `timestamp`

---

### 🖥 Operator Dashboard

* Live machine status
* Latest telemetry values
* Alarm display
* Pass / Fail counters
* Recent telemetry history
* Device heartbeat / last seen tracking

---

### 🚨 Alarm & Validation System

* Threshold-based alarm detection
* Input validation (reject invalid payloads)
* Clear API error responses (HTTP 400)

---

## 🔌 API Endpoints

### Telemetry

```
POST /api/telemetry
GET  /api/telemetry/latest
GET  /api/telemetry/recent?limit=100
```

### System Overview

```
GET /api/stations/summary
GET /api/alarms/active
```

---

## 🚀 Getting Started

### Requirements

* .NET 8 SDK
* PostgreSQL
* ESP8266 (optional, but recommended)

---

### 1. Clone repository

```bash
git clone https://github.com/DenisToxic/MMC-Application.git
cd MMC-Application
```

---

### 2. Configure database

Update `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=iotdb;Username=iotuser;Password=iotpass"
  }
}
```

---

### 3. Run migrations

```bash
dotnet ef database update
```

---

### 4. Start backend

```bash
dotnet run
```

Swagger available at:

```
https://localhost:xxxx/swagger
```

---

### 5. Configure ESP8266

Update firmware endpoint:

```cpp
const char* serverUrl = "http://YOUR_PC_IP:5000/api/telemetry";
```

Flash and run the device.

---

## 🧪 Testing

* Send sample telemetry via Swagger or Postman
* Verify data persistence in PostgreSQL
* Check dashboard updates
* Validate error handling with invalid payloads

---

## 🔐 Security & Best Practices

* Device input treated as **untrusted**
* Validation enforced at API boundary
* Secrets (WiFi, credentials) excluded via `.gitignore`
* Clean repository structure (no build artifacts)

---

## 🧠 Design Principles

### ⚙️ Separation of concerns

* Controllers → API layer
* Services → business logic
* Data → EF Core / persistence

---

### 📡 Event-driven thinking (foundation)

* telemetry ingestion → processing → storage
* prepared for future event/alert system

---

### 🏭 Industrial mindset

* traceability via timestamps & cycles
* alarm-based system behavior
* machine-centric data model

---

## 📌 Project Goal

This project is designed as a **portfolio demonstration of industrial software engineering concepts**, including:

* .NET backend development
* embedded device integration
* telemetry processing
* monitoring & visualization systems
* database-driven traceability

---

## 🧠 Summary

This project showcases a **full-stack industrial monitoring system** combining:

* embedded systems
* backend engineering
* data persistence
* operator visualization

It reflects real-world patterns used in **automation, testing, and manufacturing software systems**.
