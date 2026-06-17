using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LooksRatingApi.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TheBestWeek_User_UserId",
                table: "TheBestWeek");

            migrationBuilder.DropForeignKey(
                name: "FK_User_PhotoUser_PhotoUserId",
                table: "User");

            migrationBuilder.DropIndex(
                name: "IX_User_PhotoUserId",
                table: "User");

            migrationBuilder.DropIndex(
                name: "IX_TheBestWeek_UserId",
                table: "TheBestWeek");

            migrationBuilder.DropColumn(
                name: "TimesInTop",
                table: "User");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "TheBestWeek");

            migrationBuilder.AlterColumn<string>(
                name: "Rank",
                table: "PhotoUser",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "PhotoUser",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_User_PhotoUserId",
                table: "User",
                column: "PhotoUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PhotoUser_User_UserId",
                table: "PhotoUser",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_User_PhotoUser_PhotoUserId",
                table: "User",
                column: "PhotoUserId",
                principalTable: "PhotoUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhotoUser_User_UserId",
                table: "PhotoUser");

            migrationBuilder.DropForeignKey(
                name: "FK_User_PhotoUser_PhotoUserId",
                table: "User");

            migrationBuilder.DropIndex(
                name: "IX_User_PhotoUserId",
                table: "User");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "PhotoUser");

            migrationBuilder.AddColumn<int>(
                name: "TimesInTop",
                table: "User",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "TheBestWeek",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Rank",
                table: "PhotoUser",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.CreateIndex(
                name: "IX_User_PhotoUserId",
                table: "User",
                column: "PhotoUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TheBestWeek_UserId",
                table: "TheBestWeek",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TheBestWeek_User_UserId",
                table: "TheBestWeek",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_User_PhotoUser_PhotoUserId",
                table: "User",
                column: "PhotoUserId",
                principalTable: "PhotoUser",
                principalColumn: "Id");
        }
    }
}
