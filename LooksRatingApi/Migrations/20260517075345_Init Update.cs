using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LooksRatingApi.Migrations
{
    /// <inheritdoc />
    public partial class InitUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent cleanup: older "UpdatePhotoUserTheBestWeek" mistakenly added shadow column SeasonId1.
            // Fresh installs never create it; existing DBs may still have it before this runs.
            migrationBuilder.Sql(
                """
                ALTER TABLE "PhotoUser" DROP CONSTRAINT IF EXISTS "FK_PhotoUser_Season_SeasonId1";

                DROP INDEX IF EXISTS "IX_PhotoUser_SeasonId1";

                ALTER TABLE "PhotoUser" DROP COLUMN IF EXISTS "SeasonId1";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SeasonId1",
                table: "PhotoUser",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhotoUser_SeasonId1",
                table: "PhotoUser",
                column: "SeasonId1");

            migrationBuilder.AddForeignKey(
                name: "FK_PhotoUser_Season_SeasonId1",
                table: "PhotoUser",
                column: "SeasonId1",
                principalTable: "Season",
                principalColumn: "Id");
        }
    }
}
