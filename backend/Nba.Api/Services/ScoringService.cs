using Microsoft.EntityFrameworkCore;
using Nba.Api.Data;
using Nba.Api.Dtos;
using Nba.Api.Models;

namespace Nba.Api.Services;

public record ScoringOutcome(ScorecardDto? Scorecard, int StatusCode, string? Error)
{
    public static ScoringOutcome Ok(ScorecardDto s) => new(s, 200, null);
    public static ScoringOutcome Fail(int code, string error) => new(null, code, error);
}

// Records scoring events and keeps Games.HomeScore/AwayScore equal to the sum of the events.
// Each operation is a single SaveChanges (one transaction), guarded by Games.RowVersion.
public class ScoringService(NbaDbContext db, ScorecardService scorecards)
{
    public async Task<ScoringOutcome> RecordAsync(
        int gameId, int playerId, int points, CancellationToken ct, int? quarter = null, int? clockSeconds = null)
    {
        if (points is not (2 or 3)) return ScoringOutcome.Fail(400, "Points must be 2 or 3.");

        var game = await db.Games.FirstOrDefaultAsync(g => g.Id == gameId, ct);
        if (game is null) return ScoringOutcome.Fail(404, "Game not found.");

        var (home, away, count) = await Totals(gameId, null, ct);
        if (IsLocked(game, count)) return ScoringOutcome.Fail(400, "The game is final; scoring is locked.");

        var stint = await db.TeamPlayers.AsNoTracking().FirstOrDefaultAsync(tp =>
            tp.PlayerId == playerId && tp.EndDate == null &&
            (tp.TeamId == game.HomeTeamId || tp.TeamId == game.AwayTeamId), ct);
        if (stint is null)
            return ScoringOutcome.Fail(400, "Player is not on either team's roster for this game.");

        // The clock is kept by the browser, so it reports when the basket happened; otherwise use the saved state.
        var q = quarter ?? game.Quarter;
        var clock = clockSeconds ?? game.ClockSecondsRemaining;
        if (ClockError(q, clock) is { } clockError) return ScoringOutcome.Fail(400, clockError);

        db.ScoringEvents.Add(new ScoringEvent
        {
            GameId = gameId, TeamId = stint.TeamId, PlayerId = playerId, Points = (byte)points,
            Quarter = (byte)q, ClockSecondsRemaining = clock, CreatedAt = DateTime.UtcNow,
        });
        game.Quarter = (byte)q;
        game.ClockSecondsRemaining = clock;

        if (stint.TeamId == game.HomeTeamId) home += points; else away += points;
        game.HomeScore = home;
        game.AwayScore = away;
        game.Status = GameStatus.InProgress;

        return await SaveAsync(gameId, ct);
    }

    // Saves the game clock (quarter and seconds left) so a reload restores it.
    public async Task<ScoringOutcome> SetClockAsync(int gameId, int quarter, int clockSeconds, CancellationToken ct)
    {
        var game = await db.Games.FirstOrDefaultAsync(g => g.Id == gameId, ct);
        if (game is null) return ScoringOutcome.Fail(404, "Game not found.");
        if (ClockError(quarter, clockSeconds) is { } error) return ScoringOutcome.Fail(400, error);

        var (_, _, count) = await Totals(gameId, null, ct);
        if (IsLocked(game, count)) return ScoringOutcome.Fail(400, "The game is final; the clock is locked.");

        game.Quarter = (byte)quarter;
        game.ClockSecondsRemaining = clockSeconds;
        return await SaveAsync(gameId, ct);
    }

    // Regulation quarters are 12:00, overtimes are 5:00.
    private static string? ClockError(int quarter, int clockSeconds)
    {
        if (quarter is < 1 or > 10) return "Quarter must be between 1 and 10.";
        var max = quarter <= 4 ? 720 : 300;
        return clockSeconds < 0 || clockSeconds > max
            ? $"Clock must be between 0 and {max} seconds in {(quarter <= 4 ? "a quarter" : "overtime")}."
            : null;
    }

    public async Task<ScoringOutcome> UndoLastAsync(int gameId, CancellationToken ct)
    {
        var game = await db.Games.FirstOrDefaultAsync(g => g.Id == gameId, ct);
        if (game is null) return ScoringOutcome.Fail(404, "Game not found.");

        var last = await db.ScoringEvents
            .Where(e => e.GameId == gameId)
            .OrderByDescending(e => e.CreatedAt).ThenByDescending(e => e.Id)
            .FirstOrDefaultAsync(ct);
        if (last is null) return ScoringOutcome.Fail(400, "There is nothing to undo.");
        if (game.Status == GameStatus.Final) return ScoringOutcome.Fail(400, "The game is final; scoring is locked.");

        var (home, away, count) = await Totals(gameId, last.Id, ct);
        db.ScoringEvents.Remove(last);

        if (count == 0)
        {
            // Back to the state before scoring began.
            game.HomeScore = null;
            game.AwayScore = null;
            game.Status = game.GameDate < DateTime.UtcNow ? GameStatus.Final : GameStatus.Scheduled;
        }
        else
        {
            game.HomeScore = home;
            game.AwayScore = away;
        }

        return await SaveAsync(gameId, ct);
    }

    public async Task<ScoringOutcome> FinishAsync(int gameId, CancellationToken ct)
    {
        var game = await db.Games.FirstOrDefaultAsync(g => g.Id == gameId, ct);
        if (game is null) return ScoringOutcome.Fail(404, "Game not found.");

        var (home, away, count) = await Totals(gameId, null, ct);
        if (count == 0) return ScoringOutcome.Fail(400, "Record at least one basket before finishing the game.");
        if (IsLocked(game, count)) return ScoringOutcome.Fail(400, "The game is already final.");
        if (home == away) return ScoringOutcome.Fail(400, "NBA games cannot end in a tie.");

        game.HomeScore = home;
        game.AwayScore = away;
        game.Status = GameStatus.Final;

        return await SaveAsync(gameId, ct);
    }

    private static bool IsLocked(Game game, int eventCount) => game.Status == GameStatus.Final && eventCount > 0;

    // Sums of points per side and the event count, optionally ignoring one event (the one being undone).
    private async Task<(int Home, int Away, int Count)> Totals(int gameId, int? excludeEventId, CancellationToken ct)
    {
        var homeTeamId = await db.Games.Where(g => g.Id == gameId).Select(g => g.HomeTeamId).FirstAsync(ct);
        var events = db.ScoringEvents.AsNoTracking().Where(e => e.GameId == gameId);
        if (excludeEventId is int excluded) events = events.Where(e => e.Id != excluded);

        var rows = await events
            .GroupBy(e => e.TeamId == homeTeamId)
            .Select(g => new { IsHome = g.Key, Points = g.Sum(x => (int)x.Points), Count = g.Count() })
            .ToListAsync(ct);

        var home = rows.FirstOrDefault(r => r.IsHome);
        var away = rows.FirstOrDefault(r => !r.IsHome);
        return (home?.Points ?? 0, away?.Points ?? 0, (home?.Count ?? 0) + (away?.Count ?? 0));
    }

    private async Task<ScoringOutcome> SaveAsync(int gameId, CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ScoringOutcome.Fail(409, "The game was updated by someone else. Reload and try again.");
        }

        db.ChangeTracker.Clear();
        return ScoringOutcome.Ok((await scorecards.BuildAsync(gameId, ct))!);
    }
}
