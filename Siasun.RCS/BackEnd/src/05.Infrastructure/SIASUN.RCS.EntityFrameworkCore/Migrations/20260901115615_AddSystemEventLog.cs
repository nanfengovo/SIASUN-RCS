using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIASUN.RCS.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemEventLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppEntityAuditRules_Priority_IsEnabled",
                table: "AppEntityAuditRules");

            migrationBuilder.AlterColumn<string>(
                name: "ExcludedProperties",
                table: "AppEntityAuditRules",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1024)",
                oldMaxLength: 1024,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "AppSystemEventLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventCategory = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Level = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    ActionDetails = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSystemEventLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppEntityAuditRules_Priority",
                table: "AppEntityAuditRules",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_AppSystemEventLogs_CreationTime",
                table: "AppSystemEventLogs",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_AppSystemEventLogs_EventCategory",
                table: "AppSystemEventLogs",
                column: "EventCategory");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSystemEventLogs");

            migrationBuilder.DropIndex(
                name: "IX_AppEntityAuditRules_Priority",
                table: "AppEntityAuditRules");

            migrationBuilder.AlterColumn<string>(
                name: "ExcludedProperties",
                table: "AppEntityAuditRules",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppEntityAuditRules_Priority_IsEnabled",
                table: "AppEntityAuditRules",
                columns: new[] { "Priority", "IsEnabled" });
        }
    }
}
