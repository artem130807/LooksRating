using LooksRatingApi;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LooksRatingApi.Migrations
{
    [DbContext(typeof(LooksRatingDbContext))]
    [Migration("20260612120000_SyncSparksWalletAndEventStore")]
    public partial class SyncSparksWalletAndEventStore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS "IX_SparksLedger_UserId_Type_CreatedAt";
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE "User" DROP COLUMN IF EXISTS "SparksCount";
                """);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'SparksLedger' AND column_name = 'Amount'
                    ) AND NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'SparksLedger' AND column_name = 'SparksCount'
                    ) THEN
                        ALTER TABLE "SparksLedger" RENAME COLUMN "Amount" TO "SparksCount";
                    END IF;

                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'SparksLedger' AND column_name = 'Type'
                    ) THEN
                        ALTER TABLE "SparksLedger" DROP COLUMN "Type";
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_SparksLedger_UserId"
                    ON "SparksLedger" ("UserId");
                """);

            migrationBuilder.CreateTable(
                name: "EventStores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EventData = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventStores", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventStores_AggregateId_Version",
                table: "EventStores",
                columns: new[] { "AggregateId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventStores_OccurredAt",
                table: "EventStores",
                column: "OccurredAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "EventStores");

            migrationBuilder.DropIndex(
                name: "IX_SparksLedger_UserId",
                table: "SparksLedger");

            migrationBuilder.AddColumn<decimal>(
                name: "SparksCount",
                table: "User",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "SparksLedger",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.RenameColumn(
                name: "SparksCount",
                table: "SparksLedger",
                newName: "Amount");

            migrationBuilder.CreateIndex(
                name: "IX_SparksLedger_UserId_Type_CreatedAt",
                table: "SparksLedger",
                columns: new[] { "UserId", "Type", "CreatedAt" });
        }
    }
}
