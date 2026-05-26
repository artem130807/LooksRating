using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LooksRatingApi.Migrations
{
    /// <inheritdoc />
    public partial class RemovePhotoSeasonAndTopPhotoSeason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TopPhotoSeason");

            migrationBuilder.DropTable(
                name: "PhotoSeason");

            migrationBuilder.DropColumn(
                name: "City",
                table: "TheBestWeek");

            migrationBuilder.RenameColumn(
                name: "GenderEnumed",
                table: "TheBestWeek",
                newName: "Week");

            migrationBuilder.AlterColumn<Guid>(
                name: "TheBestWeekId",
                table: "PhotoUser",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Rank",
                table: "PhotoUser",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "SeasonId",
                table: "PhotoUser",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_PhotoUser_SeasonId",
                table: "PhotoUser",
                column: "SeasonId");

            migrationBuilder.AddForeignKey(
                name: "FK_PhotoUser_Season_SeasonId",
                table: "PhotoUser",
                column: "SeasonId",
                principalTable: "Season",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhotoUser_Season_SeasonId",
                table: "PhotoUser");

            migrationBuilder.DropIndex(
                name: "IX_PhotoUser_SeasonId",
                table: "PhotoUser");

            migrationBuilder.DropColumn(
                name: "Rank",
                table: "PhotoUser");

            migrationBuilder.DropColumn(
                name: "SeasonId",
                table: "PhotoUser");

            migrationBuilder.RenameColumn(
                name: "Week",
                table: "TheBestWeek",
                newName: "GenderEnumed");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "TheBestWeek",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<Guid>(
                name: "TheBestWeekId",
                table: "PhotoUser",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateTable(
                name: "PhotoSeason",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SeasonId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rank = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Rating = table.Column<decimal>(type: "numeric(4,2)", nullable: false),
                    RatingCount = table.Column<int>(type: "integer", nullable: false),
                    SnapshotAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TelegramFileId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhotoSeason", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhotoSeason_Season_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Season",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhotoSeason_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TopPhotoSeason",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PhotoSeasonId = table.Column<Guid>(type: "uuid", nullable: false),
                    GenderEnum = table.Column<int>(type: "integer", nullable: false),
                    Place = table.Column<int>(type: "integer", nullable: false),
                    City = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopPhotoSeason", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TopPhotoSeason_PhotoSeason_PhotoSeasonId",
                        column: x => x.PhotoSeasonId,
                        principalTable: "PhotoSeason",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PhotoSeason_SeasonId",
                table: "PhotoSeason",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoSeason_UserId",
                table: "PhotoSeason",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TopPhotoSeason_PhotoSeasonId_GenderEnum",
                table: "TopPhotoSeason",
                columns: new[] { "PhotoSeasonId", "GenderEnum" },
                unique: true);
        }
    }
}
