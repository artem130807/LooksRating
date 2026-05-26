using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LooksRatingApi.Migrations
{
    public partial class EnsureTheBestWeekAndUserSessionSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "TheBestWeek" ADD COLUMN IF NOT EXISTS "City" character varying(255) NOT NULL DEFAULT '';
                ALTER TABLE "TheBestWeek" ADD COLUMN IF NOT EXISTS "Year" integer NOT NULL DEFAULT 0;
                ALTER TABLE "TheBestWeek" ADD COLUMN IF NOT EXISTS "WeekOfYear" integer NOT NULL DEFAULT 0;

                CREATE UNIQUE INDEX IF NOT EXISTS "IX_TheBestWeek_City_Year_WeekOfYear"
                    ON "TheBestWeek" ("City", "Year", "WeekOfYear");

                DROP INDEX IF EXISTS "IX_UserSession_TelegramId";

                CREATE UNIQUE INDEX IF NOT EXISTS "IX_UserSession_TelegramId"
                    ON "UserSession" ("TelegramId");
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS "IX_TheBestWeek_City_Year_WeekOfYear";
                ALTER TABLE "TheBestWeek" DROP COLUMN IF EXISTS "WeekOfYear";
                ALTER TABLE "TheBestWeek" DROP COLUMN IF EXISTS "Year";
                ALTER TABLE "TheBestWeek" DROP COLUMN IF EXISTS "City";

                DROP INDEX IF EXISTS "IX_UserSession_TelegramId";
                CREATE INDEX IF NOT EXISTS "IX_UserSession_TelegramId" ON "UserSession" ("TelegramId");
                """);
        }
    }
}
