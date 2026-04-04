using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FunMasters.Data.Migrations
{
    /// <inheritdoc />
    public partial class SteamPlaytime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SteamId",
                table: "AspNetUsers",
                type: "character varying(17)",
                maxLength: 17,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SteamPlaytimes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SuggestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Playtime2WeeksMinutes = table.Column<int>(type: "integer", nullable: true),
                    PlaytimeForeverMinutes = table.Column<int>(type: "integer", nullable: true),
                    CapturedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ForeverUpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamPlaytimes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SteamPlaytimes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SteamPlaytimes_Suggestions_SuggestionId",
                        column: x => x.SuggestionId,
                        principalTable: "Suggestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SteamPlaytimes_SuggestionId_UserId",
                table: "SteamPlaytimes",
                columns: new[] { "SuggestionId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SteamPlaytimes_UserId",
                table: "SteamPlaytimes",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SteamPlaytimes");

            migrationBuilder.DropColumn(
                name: "SteamId",
                table: "AspNetUsers");
        }
    }
}
