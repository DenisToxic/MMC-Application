using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MMC_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddStationStateAndProductionEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "production_events",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DeviceId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StationId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EventType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EventCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Message = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PreviousState = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    NewState = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    TelemetryRecordId = table.Column<long>(type: "bigint", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "station_states",
                columns: table => new
                {
                    DeviceId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StationId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CurrentState = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LatestTelemetryRecordId = table.Column<long>(type: "bigint", nullable: true),
                    LastCycleCount = table.Column<long>(type: "bigint", nullable: false),
                    LastAlarmCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    LastSeenUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_station_states", x => new { x.StationId, x.DeviceId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_production_events_OccurredAtUtc",
                table: "production_events",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_production_events_StationId_DeviceId_OccurredAtUtc",
                table: "production_events",
                columns: new[] { "StationId", "DeviceId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_station_states_LastSeenUtc",
                table: "station_states",
                column: "LastSeenUtc");

            migrationBuilder.Sql("""
                INSERT INTO station_states ("StationId", "DeviceId", "CurrentState", "LatestTelemetryRecordId", "LastCycleCount", "LastAlarmCode", "LastSeenUtc", "UpdatedAtUtc")
                SELECT DISTINCT ON ("StationId", "DeviceId")
                    "StationId",
                    "DeviceId",
                    CASE
                        WHEN "AlarmCode" IS NOT NULL OR "TestResult" = 'Fail' THEN 'Fault'
                        WHEN "TestResult" = 'Running' OR "LoadPercent" > 0 THEN 'Running'
                        ELSE 'Idle'
                    END,
                    "Id",
                    "CycleCount",
                    "AlarmCode",
                    "ReceivedAtUtc",
                    "ReceivedAtUtc"
                FROM telemetry_records
                ORDER BY "StationId", "DeviceId", "ReceivedAtUtc" DESC;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "production_events");

            migrationBuilder.DropTable(
                name: "station_states");
        }
    }
}
