using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LooksRatingApi.Migrations
{
    /// <inheritdoc />
    public partial class PhotoUserUniquePerSeason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PhotoUser_UserId",
                table: "PhotoUser");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoUser_UserId_SeasonId",
                table: "PhotoUser",
                columns: new[] { "UserId", "SeasonId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PhotoUser_UserId_SeasonId",
                table: "PhotoUser");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoUser_UserId",
                table: "PhotoUser",
                column: "UserId",
                unique: true);
        }
    }
}
