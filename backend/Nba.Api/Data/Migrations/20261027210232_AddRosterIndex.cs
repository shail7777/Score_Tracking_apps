using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nba.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRosterIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TeamPlayers_TeamId_EndDate",
                table: "TeamPlayers",
                columns: new[] { "TeamId", "EndDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TeamPlayers_TeamId_EndDate",
                table: "TeamPlayers");
        }
    }
}
