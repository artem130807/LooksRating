using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LooksRatingApi.Migrations
{
    public partial class MigratePhotoUserToPhotoProfile : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO "PhotoProfile" ("Id", "UserId", "SeasonId", "Rating", "RatingCount", "Rank", "Status", "City", "AgeNomination", "GenderNomination", "CreatedAt")
                SELECT DISTINCT ON (p."UserId", p."SeasonId")
                       gen_random_uuid(),
                       p."UserId",
                       p."SeasonId",
                       p."Rating",
                       p."RatingCount",
                       p."Rank",
                       p."Status",
                       p."City",
                       p."AgeNomination",
                       p."GenderNomination",
                       p."CreatedAt"
                FROM "PhotoUser" p
                ORDER BY p."UserId", p."SeasonId", p."RatingCount" DESC, p."CreatedAt" DESC;
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO "PhotoProfilePhoto" ("Id", "PhotoProfileId", "TelegramFileId", "SortOrder", "CreatedAt")
                SELECT pu."Id",
                       pp."Id",
                       pu."TelegramFileId",
                       x."SortOrder",
                       pu."CreatedAt"
                FROM (
                    SELECT p.*,
                           ROW_NUMBER() OVER (PARTITION BY p."UserId", p."SeasonId" ORDER BY p."CreatedAt", p."Id") - 1 AS "SortOrder"
                    FROM "PhotoUser" p
                ) x
                JOIN "PhotoUser" pu ON pu."Id" = x."Id"
                JOIN "PhotoProfile" pp ON pp."UserId" = pu."UserId" AND pp."SeasonId" = pu."SeasonId"
                WHERE x."SortOrder" < 4;
                """);

            migrationBuilder.Sql(
                """
                UPDATE "PhotoUser" pu
                SET "PhotoProfileId" = pp."Id"
                FROM "PhotoProfile" pp
                WHERE pp."UserId" = pu."UserId"
                  AND pp."SeasonId" = pu."SeasonId";
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Review" r
                SET "PhotoProfileId" = pp."Id"
                FROM "PhotoUser" pu
                JOIN "PhotoProfile" pp ON pp."UserId" = pu."UserId" AND pp."SeasonId" = pu."SeasonId"
                WHERE r."PhotoUserId" = pu."Id";
                """);

            migrationBuilder.Sql(
                """
                UPDATE "UserTicket" t
                SET "PhotoProfileId" = pp."Id"
                FROM "PhotoUser" pu
                JOIN "PhotoProfile" pp ON pp."UserId" = pu."UserId" AND pp."SeasonId" = pu."SeasonId"
                WHERE t."PhotoUserId" = pu."Id";
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM "Review" r
                USING "Review" d
                WHERE r."Id" > d."Id"
                  AND r."UserId" = d."UserId"
                  AND r."PhotoProfileId" = d."PhotoProfileId"
                  AND r."PhotoProfileId" IS NOT NULL;
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM "UserTicket" r
                USING "UserTicket" d
                WHERE r."Id" > d."Id"
                  AND r."UserId" = d."UserId"
                  AND r."PhotoProfileId" = d."PhotoProfileId"
                  AND r."PhotoProfileId" IS NOT NULL;
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM "Review"
                WHERE "PhotoProfileId" IS NULL;
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM "UserTicket"
                WHERE "PhotoProfileId" IS NULL;
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE "Review"
                ALTER COLUMN "PhotoProfileId" SET NOT NULL;
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE "UserTicket"
                ALTER COLUMN "PhotoProfileId" SET NOT NULL;
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE "Review"
                ADD CONSTRAINT "FK_Review_PhotoProfile_PhotoProfileId"
                FOREIGN KEY ("PhotoProfileId") REFERENCES "PhotoProfile" ("Id") ON DELETE CASCADE;
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE "UserTicket"
                ADD CONSTRAINT "FK_UserTicket_PhotoProfile_PhotoProfileId"
                FOREIGN KEY ("PhotoProfileId") REFERENCES "PhotoProfile" ("Id") ON DELETE CASCADE;
                """);

            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS "IX_UserTicket_UserId_PhotoProfileId";
                """);

            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX "IX_UserTicket_UserId_PhotoProfileId"
                ON "UserTicket" ("UserId", "PhotoProfileId");
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
