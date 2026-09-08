using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIASUN.RCS.Migrations
{
    /// <inheritdoc />
    public partial class Add_AgvBatch_And_Carrier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WorkflowKey",
                table: "AppAgvTasks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TotalSubTasks = table.Column<int>(type: "int", nullable: false),
                    CompletedSubTasks = table.Column<int>(type: "int", nullable: false),
                    FailedSubTasks = table.Column<int>(type: "int", nullable: false),
                    CarrierCodes = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    SourceStation = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TargetStation = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TraceId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
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
                    table.PrimaryKey("PK_AppBatches", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppBatches_BatchCode",
                table: "AppBatches",
                column: "BatchCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppBatches_CreationTime",
                table: "AppBatches",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_AppBatches_Status",
                table: "AppBatches",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppBatches");

            migrationBuilder.DropColumn(
                name: "WorkflowKey",
                table: "AppAgvTasks");
        }
    }
}
