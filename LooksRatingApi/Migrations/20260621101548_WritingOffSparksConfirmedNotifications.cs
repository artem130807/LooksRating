using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LooksRatingApi.Migrations
{
    /// <inheritdoc />
    public partial class WritingOffSparksConfirmedNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WritingOffSparksConfirmedNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WritingOffSparksId = table.Column<Guid>(type: "uuid", nullable: false),
                    TelegramId = table.Column<long>(type: "bigint", nullable: false),
                    Stars = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WritingOffSparksConfirmedNotifications", x => x.Id);
                });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WritingOffSparksConfirmedNotifications");
        }
    }
}
