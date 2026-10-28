namespace Nba.Api.Models;

public class Player
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int? HeightInches { get; set; }
    public int? WeightLbs { get; set; }
    public DateOnly? BirthDate { get; set; }

    public List<TeamPlayer> TeamPlayers { get; set; } = new();
}
