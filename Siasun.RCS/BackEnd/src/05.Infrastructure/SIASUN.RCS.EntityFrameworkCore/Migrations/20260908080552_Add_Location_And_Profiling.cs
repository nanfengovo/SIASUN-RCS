using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIASUN.RCS.Migrations
{
    /// <inheritdoc />
    public partial class Add_Location_And_Profiling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ElapsedMilliseconds",
                table: "AppOperationLogs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OptionCodeSchemaCode",
                table: "AppAgvTasks",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OptionCodeSchemaVersion",
                table: "AppAgvTasks",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppLocationLocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LockType = table.Column<int>(type: "int", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LeaseExpirationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppLocationLocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppLocationMaps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    StationCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PreDockStationCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    MapCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Heading = table.Column<double>(type: "float", nullable: true),
                    Area = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppLocationMaps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppLocationPlcConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    GatewayId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    MaterialPresenceTag = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    InterlockReadyTag = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    TimeoutSeconds = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppLocationPlcConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppTaskStepProfilings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BatchId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    AgvId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    StepIndex = table.Column<int>(type: "int", nullable: false),
                    ActiveLeg = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Subsystem = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    OperationName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    TraceId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppTaskStepProfilings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppOperationLogs_ElapsedMilliseconds",
                table: "AppOperationLogs",
                column: "ElapsedMilliseconds");

            migrationBuilder.CreateIndex(
                name: "IX_AppLocationLocks_LeaseExpirationTime",
                table: "AppLocationLocks",
                column: "LeaseExpirationTime");

            migrationBuilder.CreateIndex(
                name: "IX_AppLocationLocks_LocationCode",
                table: "AppLocationLocks",
                column: "LocationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppLocationLocks_TaskId",
                table: "AppLocationLocks",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_AppLocationMaps_Area",
                table: "AppLocationMaps",
                column: "Area");

            migrationBuilder.CreateIndex(
                name: "IX_AppLocationMaps_IsEnabled",
                table: "AppLocationMaps",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_AppLocationMaps_LocationCode",
                table: "AppLocationMaps",
                column: "LocationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppLocationMaps_StationCode",
                table: "AppLocationMaps",
                column: "StationCode");

            migrationBuilder.CreateIndex(
                name: "IX_AppLocationPlcConfigs_IsEnabled",
                table: "AppLocationPlcConfigs",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_AppLocationPlcConfigs_LocationCode",
                table: "AppLocationPlcConfigs",
                column: "LocationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppTaskStepProfilings_AgvId",
                table: "AppTaskStepProfilings",
                column: "AgvId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTaskStepProfilings_StartTime",
                table: "AppTaskStepProfilings",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_AppTaskStepProfilings_Subsystem",
                table: "AppTaskStepProfilings",
                column: "Subsystem");

            migrationBuilder.CreateIndex(
                name: "IX_AppTaskStepProfilings_Subsystem_DurationMs",
                table: "AppTaskStepProfilings",
                columns: new[] { "Subsystem", "DurationMs" });

            migrationBuilder.CreateIndex(
                name: "IX_AppTaskStepProfilings_TaskCode",
                table: "AppTaskStepProfilings",
                column: "TaskCode");

            migrationBuilder.CreateIndex(
                name: "IX_AppTaskStepProfilings_TaskCode_StartTime",
                table: "AppTaskStepProfilings",
                columns: new[] { "TaskCode", "StartTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppLocationLocks");

            migrationBuilder.DropTable(
                name: "AppLocationMaps");

            migrationBuilder.DropTable(
                name: "AppLocationPlcConfigs");

            migrationBuilder.DropTable(
                name: "AppTaskStepProfilings");

            migrationBuilder.DropIndex(
                name: "IX_AppOperationLogs_ElapsedMilliseconds",
                table: "AppOperationLogs");

            migrationBuilder.DropColumn(
                name: "ElapsedMilliseconds",
                table: "AppOperationLogs");

            migrationBuilder.DropColumn(
                name: "OptionCodeSchemaCode",
                table: "AppAgvTasks");

            migrationBuilder.DropColumn(
                name: "OptionCodeSchemaVersion",
                table: "AppAgvTasks");
        }
    }
}
