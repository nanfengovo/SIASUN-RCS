using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIASUN.RCS.Migrations
{
    /// <inheritdoc />
    public partial class Add_TaskSerialMapping_And_Workflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastRetryTime",
                table: "AppAgvTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxRetryCount",
                table: "AppAgvTasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "AppAgvTasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "WorkflowDefinitionId",
                table: "AppAgvTasks",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowVersion",
                table: "AppAgvTasks",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppTaskSerialMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TmSerial = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Leg = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    StepIndex = table.Column<int>(type: "int", nullable: false),
                    WaitingEvent = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    VehicleCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppTaskSerialMappings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppTaskSerialMappings_CreationTime",
                table: "AppTaskSerialMappings",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_AppTaskSerialMappings_TaskId",
                table: "AppTaskSerialMappings",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTaskSerialMappings_TmSerial",
                table: "AppTaskSerialMappings",
                column: "TmSerial",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppTaskSerialMappings");

            migrationBuilder.DropColumn(
                name: "LastRetryTime",
                table: "AppAgvTasks");

            migrationBuilder.DropColumn(
                name: "MaxRetryCount",
                table: "AppAgvTasks");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "AppAgvTasks");

            migrationBuilder.DropColumn(
                name: "WorkflowDefinitionId",
                table: "AppAgvTasks");

            migrationBuilder.DropColumn(
                name: "WorkflowVersion",
                table: "AppAgvTasks");
        }
    }
}
