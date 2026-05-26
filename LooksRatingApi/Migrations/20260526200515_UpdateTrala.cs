using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LooksRatingApi.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTrala : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhotoUserTheBestWeek");

            migrationBuilder.AddColumn<string>(
                name: "SnapshotJson",
                table: "TheBestWeek",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SnapshotJson",
                table: "TheBestWeek");

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
    }
}
