namespace Nba.Api.Models;

public enum GameStatus
{
    Scheduled,
    InProgress,
    Final
}

public class Game
{
    public int Id { get; set; }
    public string Season { get; set; } = string.Empty; // e.g. "2026-27"
    public DateTime GameDate { get; set; }
    public string? Arena { get; set; }
    public GameStatus Status { get; set; } = GameStatus.Scheduled;

    public int HomeTeamId { get; set; }
    public Team? HomeTeam { get; set; }
    public int AwayTeamId { get; set; }
    public Team? AwayTeam { get; set; }

    // Null until the game is Final.
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }

    // Game clock state, saved when the clock is paused, the quarter changes, or a basket is recorded.
    // Quarter 1-4 are regulation (12:00); 5+ are overtimes (5:00).
    public byte Quarter { get; set; } = 1;
    public int ClockSecondsRemaining { get; set; } = 720;

    // Concurrency token: stops two scorers from overwriting each other's score totals.
    public byte[]? RowVersion { get; set; }
}
