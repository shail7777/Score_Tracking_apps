using Nba.Api.Dtos;
using Nba.Api.Models;

namespace Nba.Api.Services;

// Entity -> DTO conversions shared by the services.
internal static class Mappers
{
    public static TeamDto ToDto(Team t) => new(t.Id, t.Name, t.City, t.Conference, t.Division);

    // Expects TeamPlayers to contain only the active stint.
    public static PlayerDto ToDto(Player p)
    {
        var current = p.TeamPlayers.FirstOrDefault(tp => tp.EndDate == null);
        return new(p.Id, p.FirstName, p.LastName, p.HeightInches, p.WeightLbs, p.BirthDate,
            current?.TeamId, current?.Team?.Name);
    }

    public static TeamPlayerDto ToDto(TeamPlayer tp) =>
        new(tp.Id, tp.TeamId, tp.Team?.Name ?? string.Empty, tp.PlayerId,
            tp.Player is null ? string.Empty : $"{tp.Player.FirstName} {tp.Player.LastName}",
            tp.StartDate, tp.EndDate, tp.EndDate is null);

    public static GameDto ToDto(Game g)
    {
        int? winner = g.Status == GameStatus.Final && g.HomeScore is not null && g.AwayScore is not null
            ? (g.HomeScore > g.AwayScore ? g.HomeTeamId : g.AwayTeamId)
            : null;

        return new(g.Id, g.Season, g.GameDate, g.Arena, g.Status,
            g.HomeTeamId, g.HomeTeam?.Name ?? string.Empty,
            g.AwayTeamId, g.AwayTeam?.Name ?? string.Empty,
            g.HomeScore, g.AwayScore, winner);
    }

    // A game from one team's point of view (team schedule).
    public static TeamGameDto ToTeamGameDto(Game g, int teamId)
    {
        var isHome = g.HomeTeamId == teamId;
        var opponent = isHome ? g.AwayTeam : g.HomeTeam;
        var teamScore = isHome ? g.HomeScore : g.AwayScore;
        var opponentScore = isHome ? g.AwayScore : g.HomeScore;
        string? result = g.Status == GameStatus.Final && teamScore is not null && opponentScore is not null
            ? (teamScore > opponentScore ? "W" : "L")
            : null;

        return new(g.Id, g.Season, g.GameDate, g.Arena, g.Status, isHome,
            isHome ? g.AwayTeamId : g.HomeTeamId, opponent?.Name ?? string.Empty,
            teamScore, opponentScore, result);
    }
}
