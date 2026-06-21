using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LooksRatingApi.Migrations
{
    /// <inheritdoc />
    public partial class DropWritingOffSparksNotificationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WritingOffSparksCancelledNotifications");

            migrationBuilder.DropTable(
                name: "WritingOffSparksConfirmedNotifications");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WritingOffSparksCancelledNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SparksCount = table.Column<decimal>(type: "numeric", nullable: false),
                    Stars = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TelegramId = table.Column<long>(type: "bigint", nullable: false),
                    WritingOffSparksId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WritingOffSparksCancelledNotifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WritingOffSparksConfirmedNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Stars = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TelegramId = table.Column<long>(type: "bigint", nullable: false),
                    WritingOffSparksId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WritingOffSparksConfirmedNotifications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WritingOffSparksCancelledNotifications_Status",
                table: "WritingOffSparksCancelledNotifications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WritingOffSparksCancelledNotifications_WritingOffSparksId",
                table: "WritingOffSparksCancelledNotifications",
                column: "WritingOffSparksId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WritingOffSparksConfirmedNotifications_Status",
                table: "WritingOffSparksConfirmedNotifications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WritingOffSparksConfirmedNotifications_WritingOffSparksId",
                table: "WritingOffSparksConfirmedNotifications",
                column: "WritingOffSparksId",
                unique: true);
        }
    }
}
