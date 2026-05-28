using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LooksRatingApi.Migrations
{
    /// <inheritdoc />
    public partial class NewMigrationServer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CountInTop",
                table: "User",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CountInTop",
                table: "User");
        }
    }
}
