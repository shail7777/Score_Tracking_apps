namespace Nba.Api.Models;

// A player's stint with a team. EndDate == null means the player is currently on the roster.
public class TeamPlayer
{
    public int Id { get; set; }

    public int TeamId { get; set; }
    public Team? Team { get; set; }

    public int PlayerId { get; set; }
    public Player? Player { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}
