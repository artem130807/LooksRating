using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LooksRatingApi.Migrations
{
    /// <inheritdoc />
    public partial class MoveUserFeedSettingsToRecomendationSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecomendationSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Age = table.Column<int>(type: "integer", nullable: true),
                    Gender = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    City = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecomendationSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecomendationSettings_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecomendationSettings_UserId",
                table: "RecomendationSettings",
                column: "UserId",
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO "RecomendationSettings" ("Id", "Age", "Gender", "UserId", "City")
                SELECT gen_random_uuid(), "Age", "Gender", "Id", "City"
                FROM "User"
                WHERE "City" IS NOT NULL AND "City" <> '';
                """);

            migrationBuilder.DropColumn(
                name: "Age",
                table: "User");

            migrationBuilder.DropColumn(
                name: "City",
                table: "User");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "User");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Age",
                table: "User",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "User",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Gender",
                table: "User",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE "User" u
                SET "Age" = rs."Age",
                    "Gender" = rs."Gender",
                    "City" = rs."City"
                FROM "RecomendationSettings" rs
                WHERE rs."UserId" = u."Id";
                """);

            migrationBuilder.DropTable(
                name: "RecomendationSettings");
        }
    }
}
