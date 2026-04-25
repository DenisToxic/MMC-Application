using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MMC_Backend.Migrations
{
    /// <inheritdoc />
    public partial class InitialTelemetrySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "telemetry_records",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DeviceId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StationId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CycleCount = table.Column<long>(type: "bigint", nullable: false),
                    UptimeMs = table.Column<long>(type: "bigint", nullable: false),
                    TemperatureC = table.Column<double>(type: "double precision", nullable: false),
                    VibrationMmS = table.Column<double>(type: "double precision", nullable: false),
                    LoadPercent = table.Column<int>(type: "integer", nullable: false),
                    TestResult = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    AlarmCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AlarmText = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeviceTimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReceivedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_telemetry_records", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_telemetry_records_ReceivedAtUtc",
                table: "telemetry_records",
                column: "ReceivedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_telemetry_records_StationId_DeviceId_ReceivedAtUtc",
                table: "telemetry_records",
                columns: new[] { "StationId", "DeviceId", "ReceivedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "telemetry_records");
        }
    }
}
