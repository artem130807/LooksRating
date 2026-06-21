using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LooksRatingApi.Migrations
{
    /// <inheritdoc />
    public partial class WritingOffSparksCancelledNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WritingOffSparksCancelledNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WritingOffSparksId = table.Column<Guid>(type: "uuid", nullable: false),
                    TelegramId = table.Column<long>(type: "bigint", nullable: false),
                    Stars = table.Column<int>(type: "integer", nullable: false),
                    SparksCount = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WritingOffSparksCancelledNotifications", x => x.Id);
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WritingOffSparksCancelledNotifications");
        }
    }
}
