using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LooksRatingApi.Migrations
{
    /// <inheritdoc />
    public partial class AddCreateReviewFlo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Review_UserId",
                table: "Review");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "PhotoUser",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Review_UserId_PhotoUserId",
                table: "Review",
                columns: new[] { "UserId", "PhotoUserId" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Review_Rating_Range",
                table: "Review",
                sql: "\"Rating\" >= 1 AND \"Rating\" <= 10");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Review_UserId_PhotoUserId",
                table: "Review");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Review_Rating_Range",
                table: "Review");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "PhotoUser");

            migrationBuilder.CreateIndex(
                name: "IX_Review_UserId",
                table: "Review",
                column: "UserId");
        }
    }
}
