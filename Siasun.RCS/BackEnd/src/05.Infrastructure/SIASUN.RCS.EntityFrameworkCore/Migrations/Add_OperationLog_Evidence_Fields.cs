using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIASUN.RCS.Migrations
{
    /// <inheritdoc />
    public partial class Add_OperationLog_Evidence_Fields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BeforeState",
                table: "AppOperationLogs",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AfterState",
                table: "AppOperationLogs",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "AppOperationLogs",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaskId",
                table: "AppOperationLogs",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AgvId",
                table: "AppOperationLogs",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppOperationLogs_TaskId",
                table: "AppOperationLogs",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_AppOperationLogs_AgvId",
                table: "AppOperationLogs",
                column: "AgvId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppOperationLogs_TaskId",
                table: "AppOperationLogs");

            migrationBuilder.DropIndex(
                name: "IX_AppOperationLogs_AgvId",
                table: "AppOperationLogs");

            migrationBuilder.DropColumn(
                name: "BeforeState",
                table: "AppOperationLogs");

            migrationBuilder.DropColumn(
                name: "AfterState",
                table: "AppOperationLogs");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "AppOperationLogs");

            migrationBuilder.DropColumn(
                name: "TaskId",
                table: "AppOperationLogs");

            migrationBuilder.DropColumn(
                name: "AgvId",
                table: "AppOperationLogs");
        }
    }
}

