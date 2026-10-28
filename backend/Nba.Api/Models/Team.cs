namespace Nba.Api.Models;

public class Team
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Conference { get; set; } = string.Empty;
    public string Division { get; set; } = string.Empty;

    public List<TeamPlayer> TeamPlayers { get; set; } = new();
    public List<Game> HomeGames { get; set; } = new();
    public List<Game> AwayGames { get; set; } = new();
}
