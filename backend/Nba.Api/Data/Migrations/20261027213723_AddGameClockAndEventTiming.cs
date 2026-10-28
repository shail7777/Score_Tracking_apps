using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nba.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGameClockAndEventTiming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClockSecondsRemaining",
                table: "ScoringEvents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Quarter",
                table: "ScoringEvents",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)1);

            migrationBuilder.AddColumn<int>(
                name: "ClockSecondsRemaining",
                table: "Games",
                type: "int",
                nullable: false,
                defaultValue: 720);

            migrationBuilder.AddColumn<byte>(
                name: "Quarter",
                table: "Games",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)1);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ScoringEvents_Clock",
                table: "ScoringEvents",
                sql: "[ClockSecondsRemaining] IS NULL OR [ClockSecondsRemaining] BETWEEN 0 AND 720");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ScoringEvents_Quarter",
                table: "ScoringEvents",
                sql: "[Quarter] BETWEEN 1 AND 10");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Games_Clock",
                table: "Games",
                sql: "[ClockSecondsRemaining] BETWEEN 0 AND 720");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Games_Quarter",
                table: "Games",
                sql: "[Quarter] BETWEEN 1 AND 10");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ScoringEvents_Clock",
                table: "ScoringEvents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ScoringEvents_Quarter",
                table: "ScoringEvents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Games_Clock",
                table: "Games");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Games_Quarter",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "ClockSecondsRemaining",
                table: "ScoringEvents");

            migrationBuilder.DropColumn(
                name: "Quarter",
                table: "ScoringEvents");

            migrationBuilder.DropColumn(
                name: "ClockSecondsRemaining",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "Quarter",
                table: "Games");
        }
    }
}
