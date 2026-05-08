using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FunMasters.Migrations
{
    /// <inheritdoc />
    public partial class AddHltbCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HltbCache",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    MainStory = table.Column<string>(type: "text", nullable: true),
                    MainPlusExtras = table.Column<string>(type: "text", nullable: true),
                    Completionist = table.Column<string>(type: "text", nullable: true),
                    GameUrl = table.Column<string>(type: "text", nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HltbCache", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HltbCache_Title",
                table: "HltbCache",
                column: "Title",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HltbCache");
        }
    }
}
