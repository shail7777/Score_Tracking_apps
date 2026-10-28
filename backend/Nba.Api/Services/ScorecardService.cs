using Microsoft.EntityFrameworkCore;
using Nba.Api.Data;
using Nba.Api.Dtos;
using Nba.Api.Models;

namespace Nba.Api.Services;

public class ScorecardService(NbaDbContext db)
{
    private record PlayerKey(int TeamId, int PlayerId);

    // Returns null when the game does not exist.
    public async Task<ScorecardDto?> BuildAsync(int gameId, CancellationToken ct)
    {
        var game = await db.Games.AsNoTracking()
            .Include(g => g.HomeTeam).Include(g => g.AwayTeam)
            .FirstOrDefaultAsync(g => g.Id == gameId, ct);
        if (game is null) return null;

        // One grouped query for all per-player totals.
        var totals = (await db.ScoringEvents.AsNoTracking()
                .Where(e => e.GameId == gameId)
                .GroupBy(e => new { e.TeamId, e.PlayerId })
                .Select(g => new
                {
                    g.Key.TeamId,
                    g.Key.PlayerId,
                    Points = g.Sum(x => (int)x.Points),
                    Twos = g.Count(x => x.Points == 2),
                    Threes = g.Count(x => x.Points == 3),
                })
                .ToListAsync(ct))
            .ToDictionary(t => new PlayerKey(t.TeamId, t.PlayerId));

        var teamIds = new[] { game.HomeTeamId, game.AwayTeamId };
        var names = new Dictionary<PlayerKey, (string First, string Last)>();

        // A game's roster is each team's current active roster...
        var roster = await db.TeamPlayers.AsNoTracking()
            .Where(tp => teamIds.Contains(tp.TeamId) && tp.EndDate == null)
            .Select(tp => new { tp.TeamId, tp.PlayerId, tp.Player!.FirstName, tp.Player.LastName })
            .ToListAsync(ct);
        foreach (var r in roster) names[new PlayerKey(r.TeamId, r.PlayerId)] = (r.FirstName, r.LastName);

        // ...plus anyone who already scored in this game but has since left the roster.
        var missing = totals.Keys.Where(k => !names.ContainsKey(k)).Select(k => k.PlayerId).Distinct().ToList();
        if (missing.Count > 0)
        {
            var extra = await db.Players.AsNoTracking()
                .Where(p => missing.Contains(p.Id))
                .Select(p => new { p.Id, p.FirstName, p.LastName })
                .ToListAsync(ct);
            foreach (var key in totals.Keys.Where(k => !names.ContainsKey(k)))
            {
                var p = extra.First(x => x.Id == key.PlayerId);
                names[key] = (p.FirstName, p.LastName);
            }
        }

        ScorecardTeamDto BuildTeam(int teamId, string city, string name)
        {
            var players = names
                .Where(n => n.Key.TeamId == teamId)
                .OrderBy(n => n.Value.Last, StringComparer.OrdinalIgnoreCase)
                .ThenBy(n => n.Value.First, StringComparer.OrdinalIgnoreCase)
                .Select(n =>
                {
                    totals.TryGetValue(n.Key, out var t);
                    return new ScorecardPlayerDto(
                        n.Key.PlayerId, $"{n.Value.First} {n.Value.Last}",
                        t?.Points ?? 0, t?.Twos ?? 0, t?.Threes ?? 0);
                })
                .ToList();
            return new ScorecardTeamDto(teamId, city, name, players.Sum(p => p.Points), players);
        }

        var home = BuildTeam(game.HomeTeamId, game.HomeTeam?.City ?? string.Empty, game.HomeTeam?.Name ?? string.Empty);
        var away = BuildTeam(game.AwayTeamId, game.AwayTeam?.City ?? string.Empty, game.AwayTeam?.Name ?? string.Empty);
        var eventCount = totals.Values.Sum(t => t.Twos + t.Threes);

        var isLocked = game.Status == GameStatus.Final && eventCount > 0;

        // Scoring history, oldest first so the running score can be accumulated, then returned newest first.
        var plays = await db.ScoringEvents.AsNoTracking()
            .Where(e => e.GameId == gameId)
            .OrderBy(e => e.CreatedAt).ThenBy(e => e.Id)
            .Select(e => new
            {
                e.Id, e.Quarter, e.ClockSecondsRemaining, e.TeamId, TeamName = e.Team!.Name,
                e.PlayerId, First = e.Player!.FirstName, Last = e.Player.LastName, e.Points, e.CreatedAt,
            })
            .ToListAsync(ct);
        int awayScore = 0, homeScore = 0;
        var events = new List<ScorecardEventDto>(plays.Count);
        foreach (var p in plays)
        {
            if (p.TeamId == game.HomeTeamId) homeScore += p.Points; else awayScore += p.Points;
            events.Add(new ScorecardEventDto(p.Id, p.Quarter, p.ClockSecondsRemaining, p.TeamId, p.TeamName,
                p.PlayerId, $"{p.First} {p.Last}", p.Points, awayScore, homeScore, p.CreatedAt));
        }
        events.Reverse();

        return new ScorecardDto(game.Id, game.Season, game.GameDate, game.Arena, game.Status, eventCount, isLocked,
            game.Quarter, game.ClockSecondsRemaining, away, home, events);
    }
}
