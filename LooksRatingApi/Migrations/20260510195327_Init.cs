using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LooksRatingApi.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ListSeasons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListSeasons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Season",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    IsClosed = table.Column<bool>(type: "boolean", nullable: false),
                    ListSeasonsId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Season", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Season_ListSeasons_ListSeasonsId",
                        column: x => x.ListSeasonsId,
                        principalTable: "ListSeasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhotoSeason",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TelegramFileId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Rank = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Rating = table.Column<decimal>(type: "numeric(4,2)", nullable: false),
                    RatingCount = table.Column<int>(type: "integer", nullable: false),
                    SeasonId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "TopPhotoSeason",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GenderEnum = table.Column<int>(type: "integer", nullable: false),
                    PhotoSeasonId = table.Column<Guid>(type: "uuid", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "PhotoUser",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TelegramFileId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Rating = table.Column<decimal>(type: "numeric(4,2)", nullable: false),
                    RatingCount = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TheBestWeekId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhotoUser", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TelegramId = table.Column<long>(type: "bigint", nullable: false),
                    TelegramUsername = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Age = table.Column<int>(type: "integer", nullable: true),
                    Gender = table.Column<int>(type: "integer", nullable: false),
                    TimesInTop = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    PhotoUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    City = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                    table.ForeignKey(
                        name: "FK_User_PhotoUser_PhotoUserId",
                        column: x => x.PhotoUserId,
                        principalTable: "PhotoUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Review",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PhotoUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Review", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Review_PhotoUser_PhotoUserId",
                        column: x => x.PhotoUserId,
                        principalTable: "PhotoUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Review_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TheBestWeek",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GenderEnumed = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    City = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TheBestWeek", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TheBestWeek_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserSession",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TelegramId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    State = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSession", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSession_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "UserTicket",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OccuredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTicket", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTicket_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
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
                name: "IX_PhotoUser_TheBestWeekId",
                table: "PhotoUser",
                column: "TheBestWeekId");

            migrationBuilder.CreateIndex(
                name: "IX_Review_PhotoUserId",
                table: "Review",
                column: "PhotoUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Review_UserId",
                table: "Review",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Season_ListSeasonsId",
                table: "Season",
                column: "ListSeasonsId");

            migrationBuilder.CreateIndex(
                name: "IX_TheBestWeek_UserId",
                table: "TheBestWeek",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TopPhotoSeason_PhotoSeasonId_GenderEnum",
                table: "TopPhotoSeason",
                columns: new[] { "PhotoSeasonId", "GenderEnum" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_PhotoUserId",
                table: "User",
                column: "PhotoUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_TelegramId",
                table: "User",
                column: "TelegramId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSession_TelegramId",
                table: "UserSession",
                column: "TelegramId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSession_UserId",
                table: "UserSession",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTicket_UserId",
                table: "UserTicket",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PhotoSeason_User_UserId",
                table: "PhotoSeason",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PhotoUser_TheBestWeek_TheBestWeekId",
                table: "PhotoUser",
                column: "TheBestWeekId",
                principalTable: "TheBestWeek",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TheBestWeek_User_UserId",
                table: "TheBestWeek");

            migrationBuilder.DropTable(
                name: "Review");

            migrationBuilder.DropTable(
                name: "TopPhotoSeason");

            migrationBuilder.DropTable(
                name: "UserSession");

            migrationBuilder.DropTable(
                name: "UserTicket");

            migrationBuilder.DropTable(
                name: "PhotoSeason");

            migrationBuilder.DropTable(
                name: "Season");

            migrationBuilder.DropTable(
                name: "ListSeasons");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "PhotoUser");

            migrationBuilder.DropTable(
                name: "TheBestWeek");
        }
    }
}
