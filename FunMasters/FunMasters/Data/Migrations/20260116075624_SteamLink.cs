using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FunMasters.Data.Migrations
{
    /// <inheritdoc />
    public partial class SteamLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SteamLink",
                table: "Suggestions",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SteamLink",
                table: "Suggestions");
        }
    }
}
