using Microsoft.EntityFrameworkCore;
using Nba.Api.Data;
using Nba.Api.Dtos;
using Nba.Api.Models;

namespace Nba.Api.Services;

public interface IGameService
{
    Task<IReadOnlyList<GameDto>> GetAllAsync(int? teamId, GameStatus? status, string? season, CancellationToken ct);
    Task<ServiceResult<GameDto>> GetAsync(int id, CancellationToken ct);
    Task<ServiceResult<GameDto>> CreateAsync(GameRequest request, CancellationToken ct);
    Task<ServiceResult> UpdateAsync(int id, GameRequest request, CancellationToken ct);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken ct);
}

public class GameService(NbaDbContext db) : IGameService
{
    // status=Final returns previous games (newest first); status=Scheduled returns upcoming (soonest first).
    public async Task<IReadOnlyList<GameDto>> GetAllAsync(
        int? teamId, GameStatus? status, string? season, CancellationToken ct)
    {
        var query = db.Games.AsNoTracking().Include(g => g.HomeTeam).Include(g => g.AwayTeam).AsQueryable();

        if (teamId is not null) query = query.Where(g => g.HomeTeamId == teamId || g.AwayTeamId == teamId);
        if (status is not null) query = query.Where(g => g.Status == status);
        if (!string.IsNullOrWhiteSpace(season)) query = query.Where(g => g.Season == season);

        query = status == GameStatus.Scheduled
            ? query.OrderBy(g => g.GameDate)
            : query.OrderByDescending(g => g.GameDate);

        var games = await query.ToListAsync(ct);
        return games.Select(Mappers.ToDto).ToList();
    }

    public async Task<ServiceResult<GameDto>> GetAsync(int id, CancellationToken ct)
    {
        var game = await db.Games.AsNoTracking()
            .Include(g => g.HomeTeam).Include(g => g.AwayTeam)
            .FirstOrDefaultAsync(g => g.Id == id, ct);
        return game is null ? ServiceResult<GameDto>.NotFound() : ServiceResult<GameDto>.Ok(Mappers.ToDto(game));
    }

    public async Task<ServiceResult<GameDto>> CreateAsync(GameRequest request, CancellationToken ct)
    {
        if (await Validate(request, ct) is { } error) return ServiceResult<GameDto>.BadRequest(error);

        var game = new Game();
        Apply(game, request);
        db.Games.Add(game);
        await db.SaveChangesAsync(ct);

        await db.Entry(game).Reference(g => g.HomeTeam).LoadAsync(ct);
        await db.Entry(game).Reference(g => g.AwayTeam).LoadAsync(ct);
        return ServiceResult<GameDto>.Created(Mappers.ToDto(game));
    }

    public async Task<ServiceResult> UpdateAsync(int id, GameRequest request, CancellationToken ct)
    {
        var game = await db.Games.FindAsync([id], ct);
        if (game is null) return ServiceResult.NotFound();
        if (await Validate(request, ct) is { } error) return ServiceResult.BadRequest(error);

        if (await db.ScoringEvents.AnyAsync(s => s.GameId == id, ct))
            return ServiceResult.Conflict("Game has scoring events; edit it from the scorecard instead.");

        Apply(game, request);
        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken ct)
    {
        var game = await db.Games.FindAsync([id], ct);
        if (game is null) return ServiceResult.NotFound();

        db.Games.Remove(game);
        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    // Returns an error message, or null when the request is valid.
    private async Task<string?> Validate(GameRequest r, CancellationToken ct)
    {
        if (r.HomeTeamId == r.AwayTeamId) return "Home and away teams must be different.";

        var found = await db.Teams.CountAsync(t => t.Id == r.HomeTeamId || t.Id == r.AwayTeamId, ct);
        if (found != 2) return "Home or away team does not exist.";

        if (r.Status == GameStatus.Final && (r.HomeScore is null || r.AwayScore is null))
            return "Final games require both scores.";
        if (r.Status == GameStatus.Final && r.HomeScore == r.AwayScore)
            return "NBA games cannot end in a tie.";
        if (r.Status == GameStatus.Scheduled && (r.HomeScore is not null || r.AwayScore is not null))
            return "Scheduled games cannot have scores.";

        return null;
    }

    private static void Apply(Game game, GameRequest r)
    {
        game.Season = r.Season;
        game.GameDate = r.GameDate;
        game.Arena = r.Arena;
        game.Status = r.Status;
        game.HomeTeamId = r.HomeTeamId;
        game.AwayTeamId = r.AwayTeamId;
        game.HomeScore = r.HomeScore;
        game.AwayScore = r.AwayScore;
    }
}
