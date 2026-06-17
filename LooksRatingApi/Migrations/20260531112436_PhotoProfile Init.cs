using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LooksRatingApi.Migrations
{
    /// <inheritdoc />
    public partial class PhotoProfileInit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Review_PhotoUser_PhotoUserId",
                table: "Review");

            migrationBuilder.DropForeignKey(
                name: "FK_UserTicket_PhotoUser_PhotoUserId",
                table: "UserTicket");

            migrationBuilder.DropIndex(
                name: "IX_UserTicket_UserId_PhotoUserId",
                table: "UserTicket");

            migrationBuilder.RenameColumn(
                name: "PhotoUserId",
                table: "Review",
                newName: "PhotoProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_Review_UserId_PhotoUserId",
                table: "Review",
                newName: "IX_Review_UserId_PhotoProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_Review_PhotoUserId",
                table: "Review",
                newName: "IX_Review_PhotoProfileId");

            migrationBuilder.AlterColumn<Guid>(
                name: "PhotoUserId",
                table: "UserTicket",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "PhotoProfileId",
                table: "UserTicket",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PhotoProfileId",
                table: "PhotoUser",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PhotoProfile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeasonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rating = table.Column<decimal>(type: "numeric(4,2)", nullable: false, defaultValue: 0m),
                    RatingCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AgeNomination = table.Column<int>(type: "integer", nullable: false),
                    GenderNomination = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    City = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhotoProfile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhotoProfile_Season_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Season",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhotoProfile_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhotoProfilePhoto",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PhotoProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    TelegramFileId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhotoProfilePhoto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhotoProfilePhoto_PhotoProfile_PhotoProfileId",
                        column: x => x.PhotoProfileId,
                        principalTable: "PhotoProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserTicket_PhotoProfileId",
                table: "UserTicket",
                column: "PhotoProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTicket_UserId_PhotoProfileId",
                table: "UserTicket",
                columns: new[] { "UserId", "PhotoProfileId" },
                unique: false);

            migrationBuilder.CreateIndex(
                name: "IX_PhotoUser_PhotoProfileId",
                table: "PhotoUser",
                column: "PhotoProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoProfile_SeasonId_GenderNomination_AgeNomination",
                table: "PhotoProfile",
                columns: new[] { "SeasonId", "GenderNomination", "AgeNomination" });

            migrationBuilder.CreateIndex(
                name: "IX_PhotoProfile_SeasonId_Status",
                table: "PhotoProfile",
                columns: new[] { "SeasonId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PhotoProfile_UserId_SeasonId",
                table: "PhotoProfile",
                columns: new[] { "UserId", "SeasonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhotoProfilePhoto_PhotoProfileId",
                table: "PhotoProfilePhoto",
                column: "PhotoProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoProfilePhoto_PhotoProfileId_SortOrder",
                table: "PhotoProfilePhoto",
                columns: new[] { "PhotoProfileId", "SortOrder" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PhotoUser_PhotoProfile_PhotoProfileId",
                table: "PhotoUser",
                column: "PhotoProfileId",
                principalTable: "PhotoProfile",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_UserTicket_PhotoUser_PhotoUserId",
                table: "UserTicket",
                column: "PhotoUserId",
                principalTable: "PhotoUser",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhotoUser_PhotoProfile_PhotoProfileId",
                table: "PhotoUser");

            migrationBuilder.DropForeignKey(
                name: "FK_UserTicket_PhotoUser_PhotoUserId",
                table: "UserTicket");

            migrationBuilder.DropTable(
                name: "PhotoProfilePhoto");

            migrationBuilder.DropTable(
                name: "PhotoProfile");

            migrationBuilder.DropIndex(
                name: "IX_UserTicket_PhotoProfileId",
                table: "UserTicket");

            migrationBuilder.DropIndex(
                name: "IX_UserTicket_UserId_PhotoProfileId",
                table: "UserTicket");

            migrationBuilder.DropIndex(
                name: "IX_PhotoUser_PhotoProfileId",
                table: "PhotoUser");

            migrationBuilder.DropColumn(
                name: "PhotoProfileId",
                table: "UserTicket");

            migrationBuilder.DropColumn(
                name: "PhotoProfileId",
                table: "PhotoUser");

            migrationBuilder.RenameColumn(
                name: "PhotoProfileId",
                table: "Review",
                newName: "PhotoUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Review_UserId_PhotoProfileId",
                table: "Review",
                newName: "IX_Review_UserId_PhotoUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Review_PhotoProfileId",
                table: "Review",
                newName: "IX_Review_PhotoUserId");

            migrationBuilder.AlterColumn<Guid>(
                name: "PhotoUserId",
                table: "UserTicket",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserTicket_UserId_PhotoUserId",
                table: "UserTicket",
                columns: new[] { "UserId", "PhotoUserId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Review_PhotoUser_PhotoUserId",
                table: "Review",
                column: "PhotoUserId",
                principalTable: "PhotoUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserTicket_PhotoUser_PhotoUserId",
                table: "UserTicket",
                column: "PhotoUserId",
                principalTable: "PhotoUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
