using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LooksRatingApi.Migrations
{
    public partial class RepairPhotoUserSeasonIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS "IX_PhotoUser_UserId_SeasonId";
                CREATE INDEX "IX_PhotoUser_UserId_SeasonId"
                ON "PhotoUser" ("UserId", "SeasonId");
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS "IX_PhotoUser_UserId_SeasonId";
                CREATE UNIQUE INDEX "IX_PhotoUser_UserId_SeasonId"
                ON "PhotoUser" ("UserId", "SeasonId");
                """);
        }
    }
}
