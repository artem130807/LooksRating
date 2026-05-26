using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LooksRatingApi.Migrations
{
    /// <inheritdoc />
    public partial class AddRankEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // varchar -> int: drop default first (PG cannot cast default to new type); then USING for data.
            migrationBuilder.Sql(
                """
                ALTER TABLE "PhotoUser" ALTER COLUMN "Rank" DROP DEFAULT;

                ALTER TABLE "PhotoUser" ALTER COLUMN "Rank" TYPE integer USING (
                    CASE
                        WHEN btrim("Rank"::text) ~ '^[0-9]+$' THEN btrim("Rank"::text)::integer
                        WHEN btrim("Rank"::text) = 'Ужасный' THEN 0
                        WHEN btrim("Rank"::text) = 'Некрасивый' THEN 1
                        WHEN btrim("Rank"::text) = 'Обычный' THEN 2
                        WHEN btrim("Rank"::text) = 'Симпатичный' THEN 3
                        WHEN btrim("Rank"::text) = 'Красивый' THEN 4
                        WHEN btrim("Rank"::text) = 'Прекрасна' THEN 5
                        ELSE 0
                    END
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "PhotoUser" ALTER COLUMN "Rank" DROP DEFAULT;

                ALTER TABLE "PhotoUser" ALTER COLUMN "Rank" TYPE character varying(64) USING (
                    CASE "Rank"
                        WHEN 0 THEN 'Ужасный'
                        WHEN 1 THEN 'Некрасивый'
                        WHEN 2 THEN 'Обычный'
                        WHEN 3 THEN 'Симпатичный'
                        WHEN 4 THEN 'Красивый'
                        WHEN 5 THEN 'Прекрасна'
                        ELSE 'Обычный'
                    END
                );
                """);
        }
    }
}
