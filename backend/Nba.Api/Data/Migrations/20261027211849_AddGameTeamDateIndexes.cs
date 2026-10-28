using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nba.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGameTeamDateIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Games_AwayTeamId",
                table: "Games");

            migrationBuilder.DropIndex(
                name: "IX_Games_HomeTeamId",
                table: "Games");

            migrationBuilder.CreateIndex(
                name: "IX_Games_AwayTeamId_GameDate",
                table: "Games",
                columns: new[] { "AwayTeamId", "GameDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Games_HomeTeamId_GameDate",
                table: "Games",
                columns: new[] { "HomeTeamId", "GameDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Games_AwayTeamId_GameDate",
                table: "Games");

            migrationBuilder.DropIndex(
                name: "IX_Games_HomeTeamId_GameDate",
                table: "Games");

            migrationBuilder.CreateIndex(
                name: "IX_Games_AwayTeamId",
                table: "Games",
                column: "AwayTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_HomeTeamId",
                table: "Games",
                column: "HomeTeamId");
        }
    }
}
