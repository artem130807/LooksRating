using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LooksRatingApi.Migrations
{
    /// <inheritdoc />
    public partial class AddPhotoUserIdToUserTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserTicket_UserId",
                table: "UserTicket");

            migrationBuilder.AddColumn<Guid>(
                name: "PhotoUserId",
                table: "UserTicket",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_UserTicket_PhotoUserId",
                table: "UserTicket",
                column: "PhotoUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTicket_UserId_PhotoUserId",
                table: "UserTicket",
                columns: new[] { "UserId", "PhotoUserId" },
                unique: true);

            migrationBuilder.Sql(@"DELETE FROM ""UserTicket"" WHERE ""PhotoUserId"" = '00000000-0000-0000-0000-000000000000';");

            migrationBuilder.AddForeignKey(
                name: "FK_UserTicket_PhotoUser_PhotoUserId",
                table: "UserTicket",
                column: "PhotoUserId",
                principalTable: "PhotoUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserTicket_PhotoUser_PhotoUserId",
                table: "UserTicket");

            migrationBuilder.DropIndex(
                name: "IX_UserTicket_PhotoUserId",
                table: "UserTicket");

            migrationBuilder.DropIndex(
                name: "IX_UserTicket_UserId_PhotoUserId",
                table: "UserTicket");

            migrationBuilder.DropColumn(
                name: "PhotoUserId",
                table: "UserTicket");

            migrationBuilder.CreateIndex(
                name: "IX_UserTicket_UserId",
                table: "UserTicket",
                column: "UserId");
        }
    }
}
