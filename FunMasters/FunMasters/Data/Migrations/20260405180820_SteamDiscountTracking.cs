using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FunMasters.Data.Migrations
{
    /// <inheritdoc />
    public partial class SteamDiscountTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LastKnownPriceSar",
                table: "Suggestions",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastKnownPriceSar",
                table: "Suggestions");
        }
    }
}
