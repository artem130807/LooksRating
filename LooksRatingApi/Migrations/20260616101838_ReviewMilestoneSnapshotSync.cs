using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LooksRatingApi.Migrations
{
    /// <inheritdoc />
    public partial class ReviewMilestoneSnapshotSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Review_PhotoProfileId",
                table: "Review");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Review",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW() AT TIME ZONE 'UTC'");

            migrationBuilder.CreateTable(
                name: "ReviewMilestoneNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PhotoProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerTelegramId = table.Column<long>(type: "bigint", nullable: false),
                    CycleNumber = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewMilestoneNotifications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Review_PhotoProfileId_CreatedAt",
                table: "Review",
                columns: new[] { "PhotoProfileId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReviewMilestoneNotifications_PhotoProfileId_CycleNumber",
                table: "ReviewMilestoneNotifications",
                columns: new[] { "PhotoProfileId", "CycleNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReviewMilestoneNotifications_Status",
                table: "ReviewMilestoneNotifications",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReviewMilestoneNotifications");

            migrationBuilder.DropIndex(
                name: "IX_Review_PhotoProfileId_CreatedAt",
                table: "Review");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Review");

            migrationBuilder.CreateIndex(
                name: "IX_Review_PhotoProfileId",
                table: "Review",
                column: "PhotoProfileId");
        }
    }
}
