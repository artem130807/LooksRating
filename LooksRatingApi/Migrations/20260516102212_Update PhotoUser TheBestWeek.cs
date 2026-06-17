using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LooksRatingApi.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePhotoUserTheBestWeek : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhotoUser_TheBestWeek_TheBestWeekId",
                table: "PhotoUser");

            migrationBuilder.DropIndex(
                name: "IX_PhotoUser_TheBestWeekId",
                table: "PhotoUser");

            migrationBuilder.DropColumn(
                name: "TheBestWeekId",
                table: "PhotoUser");

            migrationBuilder.CreateTable(
                name: "PhotoUserTheBestWeek",
                columns: table => new
                {
                    PhotoUsersId = table.Column<Guid>(type: "uuid", nullable: false),
                    TheBestWeeksId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhotoUserTheBestWeek", x => new { x.PhotoUsersId, x.TheBestWeeksId });
                    table.ForeignKey(
                        name: "FK_PhotoUserTheBestWeek_PhotoUser_PhotoUsersId",
                        column: x => x.PhotoUsersId,
                        principalTable: "PhotoUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhotoUserTheBestWeek_TheBestWeek_TheBestWeeksId",
                        column: x => x.TheBestWeeksId,
                        principalTable: "TheBestWeek",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PhotoUserTheBestWeek_TheBestWeeksId",
                table: "PhotoUserTheBestWeek",
                column: "TheBestWeeksId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhotoUserTheBestWeek");

            migrationBuilder.AddColumn<Guid>(
                name: "TheBestWeekId",
                table: "PhotoUser",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_PhotoUser_TheBestWeekId",
                table: "PhotoUser",
                column: "TheBestWeekId");

            migrationBuilder.AddForeignKey(
                name: "FK_PhotoUser_TheBestWeek_TheBestWeekId",
                table: "PhotoUser",
                column: "TheBestWeekId",
                principalTable: "TheBestWeek",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
