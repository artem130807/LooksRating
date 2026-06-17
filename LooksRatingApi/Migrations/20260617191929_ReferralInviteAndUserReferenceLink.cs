using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LooksRatingApi.Migrations
{
    /// <inheritdoc />
    public partial class ReferralInviteAndUserReferenceLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReferralInvite",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferrerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvitedUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferralInvite", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserReferenceLink",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Link = table.Column<string>(type: "text", nullable: false),
                    CountInvited = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserReferenceLink", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserReferenceLink_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReferralInvite_InvitedUserId",
                table: "ReferralInvite",
                column: "InvitedUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReferralInvite_ReferrerUserId",
                table: "ReferralInvite",
                column: "ReferrerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReferenceLink_UserId",
                table: "UserReferenceLink",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReferralInvite");

            migrationBuilder.DropTable(
                name: "UserReferenceLink");
        }
    }
}
