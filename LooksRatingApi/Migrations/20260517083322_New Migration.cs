using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LooksRatingApi.Migrations
{
    /// <inheritdoc />
    public partial class NewMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_User_PhotoUser_PhotoUserId",
                table: "User");

            migrationBuilder.DropIndex(
                name: "IX_User_PhotoUserId",
                table: "User");

            migrationBuilder.DropColumn(
                name: "PhotoUserId",
                table: "User");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PhotoUserId",
                table: "User",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_PhotoUserId",
                table: "User",
                column: "PhotoUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_User_PhotoUser_PhotoUserId",
                table: "User",
                column: "PhotoUserId",
                principalTable: "PhotoUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
