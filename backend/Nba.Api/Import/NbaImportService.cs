using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Nba.Api.Data;
using Nba.Api.Models;

namespace Nba.Api.Import;

public record ImportResult(int Games, int TeamsAdded, int PlayersAdded, int GamesAdded, int GamesSkipped);

// Idempotent: teams are matched by name, players by full name, games by (date, home, away).
public class NbaImportService(NbaDbContext db, NbaGraphQlClient client, ILogger<NbaImportService> log)
{
    public async Task<ImportResult> ImportAsync(CancellationToken ct)
    {
        var apiGames = await client.GetGamesAsync(ct);

        var teams = (await db.Teams.ToListAsync(ct)).ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
        var players = (await db.Players.Include(p => p.TeamPlayers).ToListAsync(ct))
            .GroupBy(p => PlayerKey(p.FirstName, p.LastName), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        int teamsAdded = 0, playersAdded = 0;

        Team? Resolve(ApiTeam? api)
        {
            var name = (api?.NickName ?? api?.ShortName)?.Trim();
            if (api is null || string.IsNullOrEmpty(name)) return null;

            var city = api.ShortName?.Trim() ?? string.Empty;
            if (city.Equals(name, StringComparison.OrdinalIgnoreCase)) city = string.Empty;

            if (!teams.TryGetValue(name, out var team))
            {
                team = new Team
                {
                    Name = Trim(name, 100),
                    City = Trim(city, 100),
                    Conference = string.Empty,
                    Division = string.Empty,
                };
                db.Teams.Add(team);
                teams[name] = team;
                teamsAdded++;
            }
            else if (string.IsNullOrEmpty(team.City) && city.Length > 0)
            {
                team.City = Trim(city, 100);
            }

            foreach (var ap in api.Players ?? [])
            {
                var first = ap.FirstName?.Trim();
                var last = ap.LastName?.Trim();
                if (string.IsNullOrEmpty(first) || string.IsNullOrEmpty(last)) continue;

                var key = PlayerKey(first, last);
                if (!players.TryGetValue(key, out var player))
                {
                    player = new Player { FirstName = Trim(first, 50), LastName = Trim(last, 50) };
                    db.Players.Add(player);
                    players[key] = player;
                    playersAdded++;
                }

                // First team seen wins; the API gives no start dates or trade information.
                if (player.TeamPlayers.Any(tp => tp.EndDate == null)) continue;
                var stint = new TeamPlayer { Team = team, Player = player, StartDate = today };
                player.TeamPlayers.Add(stint);
            }

            return team;
        }

        var resolved = new List<(ApiGame Api, Team Home, Team Away, DateTime Date)>();
        var skipped = 0;
        foreach (var g in apiGames)
        {
            var home = Resolve(g.HomeTeam);
            var away = Resolve(g.VisitorTeam);
            if (home is null || away is null || !TryParseDate(g.Date, out var date) || ReferenceEquals(home, away))
            {
                skipped++;
                continue;
            }
            resolved.Add((g, home, away, date));
        }

        // Saved first so new teams have IDs for the game de-duplication below.
        await db.SaveChangesAsync(ct);

        var existing = (await db.Games.Select(g => new { g.GameDate, g.HomeTeamId, g.AwayTeamId }).ToListAsync(ct))
            .Select(g => (g.GameDate, g.HomeTeamId, g.AwayTeamId))
            .ToHashSet();

        var now = DateTime.UtcNow;
        var gamesAdded = 0;
        foreach (var (_, home, away, date) in resolved)
        {
            if (!existing.Add((date, home.Id, away.Id))) continue;

            db.Games.Add(new Game
            {
                Season = SeasonFor(date),
                GameDate = date,
                HomeTeamId = home.Id,
                AwayTeamId = away.Id,
                // The API returns no scores, so past games are Final without a result.
                Status = date < now ? GameStatus.Final : GameStatus.Scheduled,
            });
            gamesAdded++;
        }

        await db.SaveChangesAsync(ct);

        var result = new ImportResult(apiGames.Count, teamsAdded, playersAdded, gamesAdded, skipped);
        log.LogInformation("NBA import finished: {@Result}", result);
        return result;
    }

    private static string PlayerKey(string first, string last) => $"{first} {last}";

    private static string Trim(string s, int max) => s.Length <= max ? s : s[..max];

    private static bool TryParseDate(string? s, out DateTime date) =>
        DateTime.TryParse(s, CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out date);

    // NBA seasons start in October, e.g. 2026-10-21 -> "2026-27", 2026-04-01 -> "2025-26".
    private static string SeasonFor(DateTime d)
    {
        var start = d.Month >= 10 ? d.Year : d.Year - 1;
        return $"{start}-{(start + 1) % 100:D2}";
    }
}
