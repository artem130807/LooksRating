using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LooksRatingApi.Migrations
{
    /// <inheritdoc />
    public partial class AddOutBox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    StateJson = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    NextAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProcessingStartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Type_Status_NextAttemptAt",
                table: "OutboxMessages",
                columns: new[] { "MessageType", "Status", "NextAttemptAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutboxMessages");
        }
    }
}
