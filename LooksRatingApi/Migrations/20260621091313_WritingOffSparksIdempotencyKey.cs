using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LooksRatingApi.Migrations
{
    /// <inheritdoc />
    public partial class WritingOffSparksIdempotencyKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WritingOffSparks_UserId",
                table: "WritingOffSparks");

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "WritingOffSparks",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Withdrawals_UserId_IdempotencyKey",
                table: "WritingOffSparks",
                columns: new[] { "UserId", "IdempotencyKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Withdrawals_UserId_IdempotencyKey",
                table: "WritingOffSparks");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "WritingOffSparks");

            migrationBuilder.CreateIndex(
                name: "IX_WritingOffSparks_UserId",
                table: "WritingOffSparks",
                column: "UserId");
        }
    }
}
