using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIASUN.RCS.Migrations
{
    /// <inheritdoc />
    public partial class Add_AgvTask_Vehicle_And_Evidence_Fields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var isSqlite = migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite";
            var guidType = isSqlite ? "TEXT" : "uniqueidentifier";
            var dateTimeType = isSqlite ? "TEXT" : "datetime2";
            var boolType = isSqlite ? "INTEGER" : "bit";
            var intType = isSqlite ? "INTEGER" : "int";
            var floatType = isSqlite ? "REAL" : "float";
            Func<int, string> nvarchar = len => isSqlite ? "TEXT" : $"nvarchar({len})";
            var nvarcharMax = isSqlite ? "TEXT" : "nvarchar(max)";

            migrationBuilder.AlterColumn<string>(
                name: "ErrorMessage",
                table: "AppOperationLogs",
                type: nvarchar(2048),
                maxLength: 2048,
                nullable: true,
                oldClrType: typeof(string),
                oldType: nvarchar(2048),
                oldMaxLength: 2048);

            migrationBuilder.AddColumn<string>(
                name: "AfterState",
                table: "AppOperationLogs",
                type: nvarchar(512),
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AgvId",
                table: "AppOperationLogs",
                type: nvarchar(64),
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BeforeState",
                table: "AppOperationLogs",
                type: nvarchar(512),
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "AppOperationLogs",
                type: nvarchar(512),
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaskId",
                table: "AppOperationLogs",
                type: nvarchar(64),
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppAgvTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: guidType, nullable: false),
                    TaskCode = table.Column<string>(type: nvarchar(64), maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: intType, nullable: false),
                    StepIndex = table.Column<int>(type: intType, nullable: false),
                    WaitingEvent = table.Column<string>(type: nvarchar(128), maxLength: 128, nullable: true),
                    ActiveLeg = table.Column<string>(type: nvarchar(64), maxLength: 64, nullable: true),
                    AssignedVehicleId = table.Column<Guid>(type: guidType, nullable: true),
                    AssignedVehicleCode = table.Column<string>(type: nvarchar(64), maxLength: 64, nullable: true),
                    FromStation = table.Column<string>(type: nvarchar(64), maxLength: 64, nullable: true),
                    ToStation = table.Column<string>(type: nvarchar(64), maxLength: 64, nullable: true),
                    CarrierCode = table.Column<string>(type: nvarchar(128), maxLength: 128, nullable: true),
                    BatchId = table.Column<string>(type: nvarchar(128), maxLength: 128, nullable: true),
                    OptionCode = table.Column<string>(type: nvarchar(256), maxLength: 256, nullable: true),
                    TraceId = table.Column<string>(type: nvarchar(64), maxLength: 64, nullable: true),
                    FailureReason = table.Column<string>(type: nvarchar(1024), maxLength: 1024, nullable: true),
                    StartTime = table.Column<DateTime>(type: dateTimeType, nullable: true),
                    EndTime = table.Column<DateTime>(type: dateTimeType, nullable: true),
                    ExtraProperties = table.Column<string>(type: nvarcharMax, nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: nvarchar(40), maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: dateTimeType, nullable: false),
                    CreatorId = table.Column<Guid>(type: guidType, nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: dateTimeType, nullable: true),
                    LastModifierId = table.Column<Guid>(type: guidType, nullable: true),
                    IsDeleted = table.Column<bool>(type: boolType, nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: guidType, nullable: true),
                    DeletionTime = table.Column<DateTime>(type: dateTimeType, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppAgvTasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppAgvVehicles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: guidType, nullable: false),
                    VehicleCode = table.Column<string>(type: nvarchar(64), maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: intType, nullable: false),
                    CurrentStation = table.Column<string>(type: nvarchar(64), maxLength: 64, nullable: true),
                    BatteryPercentage = table.Column<double>(type: floatType, nullable: false),
                    IpAddress = table.Column<string>(type: nvarchar(64), maxLength: 64, nullable: true),
                    ErrorMessage = table.Column<string>(type: nvarchar(1024), maxLength: 1024, nullable: true),
                    CurrentTaskId = table.Column<Guid>(type: guidType, nullable: true),
                    CurrentTaskCode = table.Column<string>(type: nvarchar(64), maxLength: 64, nullable: true),
                    ExtraProperties = table.Column<string>(type: nvarcharMax, nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: nvarchar(40), maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: dateTimeType, nullable: false),
                    CreatorId = table.Column<Guid>(type: guidType, nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: dateTimeType, nullable: true),
                    LastModifierId = table.Column<Guid>(type: guidType, nullable: true),
                    IsDeleted = table.Column<bool>(type: boolType, nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: guidType, nullable: true),
                    DeletionTime = table.Column<DateTime>(type: dateTimeType, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppAgvVehicles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppOperationLogs_AgvId",
                table: "AppOperationLogs",
                column: "AgvId");

            migrationBuilder.CreateIndex(
                name: "IX_AppOperationLogs_TaskId",
                table: "AppOperationLogs",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_AppAgvTasks_AssignedVehicleId",
                table: "AppAgvTasks",
                column: "AssignedVehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_AppAgvTasks_CreationTime",
                table: "AppAgvTasks",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_AppAgvTasks_Status",
                table: "AppAgvTasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppAgvTasks_TaskCode",
                table: "AppAgvTasks",
                column: "TaskCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppAgvTasks_TraceId",
                table: "AppAgvTasks",
                column: "TraceId");

            migrationBuilder.CreateIndex(
                name: "IX_AppAgvVehicles_CurrentTaskId",
                table: "AppAgvVehicles",
                column: "CurrentTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_AppAgvVehicles_Status",
                table: "AppAgvVehicles",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppAgvVehicles_VehicleCode",
                table: "AppAgvVehicles",
                column: "VehicleCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var isSqlite = migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite";
            Func<int, string> nvarchar = len => isSqlite ? "TEXT" : $"nvarchar({len})";

            migrationBuilder.DropTable(
                name: "AppAgvTasks");

            migrationBuilder.DropTable(
                name: "AppAgvVehicles");

            migrationBuilder.DropIndex(
                name: "IX_AppOperationLogs_AgvId",
                table: "AppOperationLogs");

            migrationBuilder.DropIndex(
                name: "IX_AppOperationLogs_TaskId",
                table: "AppOperationLogs");

            migrationBuilder.DropColumn(
                name: "AfterState",
                table: "AppOperationLogs");

            migrationBuilder.DropColumn(
                name: "AgvId",
                table: "AppOperationLogs");

            migrationBuilder.DropColumn(
                name: "BeforeState",
                table: "AppOperationLogs");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "AppOperationLogs");

            migrationBuilder.DropColumn(
                name: "TaskId",
                table: "AppOperationLogs");

            migrationBuilder.AlterColumn<string>(
                name: "ErrorMessage",
                table: "AppOperationLogs",
                type: nvarchar(2048),
                maxLength: 2048,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: nvarchar(2048),
                oldMaxLength: 2048,
                oldNullable: true);
        }
    }
}
