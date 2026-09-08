using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIASUN.RCS.Migrations
{
    /// <inheritdoc />
    public partial class Add_Outbox_And_Saga : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppOutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    MaxRetries = table.Column<int>(type: "int", nullable: false),
                    NextRetryTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Destination = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    TraceId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    ProcessedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_AppOutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppOutboxMessages_CreationTime",
                table: "AppOutboxMessages",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_AppOutboxMessages_Status_NextRetryTime",
                table: "AppOutboxMessages",
                columns: new[] { "Status", "NextRetryTime" });

            migrationBuilder.CreateIndex(
                name: "IX_AppOutboxMessages_TraceId",
                table: "AppOutboxMessages",
                column: "TraceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppOutboxMessages");
        }
    }
}
