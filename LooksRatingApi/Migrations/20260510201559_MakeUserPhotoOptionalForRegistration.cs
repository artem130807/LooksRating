using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LooksRatingApi.Migrations
{
    /// <inheritdoc />
    public partial class MakeUserPhotoOptionalForRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_User_PhotoUser_PhotoUserId",
                table: "User");

            migrationBuilder.AlterColumn<Guid>(
                name: "PhotoUserId",
                table: "User",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_User_PhotoUser_PhotoUserId",
                table: "User",
                column: "PhotoUserId",
                principalTable: "PhotoUser",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_User_PhotoUser_PhotoUserId",
                table: "User");

            migrationBuilder.AlterColumn<Guid>(
                name: "PhotoUserId",
                table: "User",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_User_PhotoUser_PhotoUserId",
                table: "User",
                column: "PhotoUserId",
                principalTable: "PhotoUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
