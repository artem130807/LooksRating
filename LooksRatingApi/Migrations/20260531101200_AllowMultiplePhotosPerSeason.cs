using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LooksRatingApi.Migrations
{
    public partial class AllowMultiplePhotosPerSeason : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PhotoUser_UserId_SeasonId",
                table: "PhotoUser");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoUser_UserId_SeasonId",
                table: "PhotoUser",
                columns: new[] { "UserId", "SeasonId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PhotoUser_UserId_SeasonId",
                table: "PhotoUser");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoUser_UserId_SeasonId",
                table: "PhotoUser",
                columns: new[] { "UserId", "SeasonId" },
                unique: true);
        }
    }
}
