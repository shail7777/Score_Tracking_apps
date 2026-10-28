namespace Nba.Api.Models;

// One basket by one player in one game. Team/player/game totals are derived from these rows.
public class ScoringEvent
{
    public int Id { get; set; }

    public int GameId { get; set; }
    public Game? Game { get; set; }

    public int TeamId { get; set; }
    public Team? Team { get; set; }

    public int PlayerId { get; set; }
    public Player? Player { get; set; }

    // 2 or 3 (enforced by a database check constraint).
    public byte Points { get; set; }

    // When the basket happened: quarter and seconds left on the game clock (null if not supplied).
    public byte Quarter { get; set; } = 1;
    public int? ClockSecondsRemaining { get; set; }

    public DateTime CreatedAt { get; set; }
}
