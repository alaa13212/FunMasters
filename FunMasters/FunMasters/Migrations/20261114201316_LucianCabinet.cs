using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FunMasters.Migrations
{
    /// <inheritdoc />
    public partial class LucianCabinet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TitleWriterGemCount",
                table: "Suggestions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TitleWriterNamedAtUtc",
                table: "Suggestions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TitleWriterUserId",
                table: "Suggestions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LongReviewAnnouncedAtUtc",
                table: "Ratings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LucianTriggerSettings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LucianTriggerSettings", x => x.Key);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Suggestions_TitleWriterUserId",
                table: "Suggestions",
                column: "TitleWriterUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Suggestions_AspNetUsers_TitleWriterUserId",
                table: "Suggestions",
                column: "TitleWriterUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Suggestions_AspNetUsers_TitleWriterUserId",
                table: "Suggestions");

            migrationBuilder.DropTable(
                name: "LucianTriggerSettings");

            migrationBuilder.DropIndex(
                name: "IX_Suggestions_TitleWriterUserId",
                table: "Suggestions");

            migrationBuilder.DropColumn(
                name: "TitleWriterGemCount",
                table: "Suggestions");

            migrationBuilder.DropColumn(
                name: "TitleWriterNamedAtUtc",
                table: "Suggestions");

            migrationBuilder.DropColumn(
                name: "TitleWriterUserId",
                table: "Suggestions");

            migrationBuilder.DropColumn(
                name: "LongReviewAnnouncedAtUtc",
                table: "Ratings");
        }
    }
}
