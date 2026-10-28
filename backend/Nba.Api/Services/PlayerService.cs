using Microsoft.EntityFrameworkCore;
using Nba.Api.Data;
using Nba.Api.Dtos;
using Nba.Api.Models;

namespace Nba.Api.Services;

public interface IPlayerService
{
    Task<IReadOnlyList<PlayerDto>> GetAllAsync(int? teamId, string? search, CancellationToken ct);
    Task<ServiceResult<PlayerDto>> GetAsync(int id, CancellationToken ct);
    Task<ServiceResult<IReadOnlyList<TeamPlayerDto>>> GetHistoryAsync(int id, CancellationToken ct);
    Task<ServiceResult<PlayerDto>> CreateAsync(PlayerRequest request, CancellationToken ct);
    Task<ServiceResult> UpdateAsync(int id, PlayerRequest request, CancellationToken ct);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken ct);
}

public class PlayerService(NbaDbContext db) : IPlayerService
{
    public async Task<IReadOnlyList<PlayerDto>> GetAllAsync(int? teamId, string? search, CancellationToken ct)
    {
        var query = WithCurrentTeam(db.Players.AsNoTracking());

        // teamId filters on the *current* team.
        if (teamId is not null)
            query = query.Where(p => p.TeamPlayers.Any(tp => tp.EndDate == null && tp.TeamId == teamId));

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.FirstName.Contains(search) || p.LastName.Contains(search));

        var players = await query.OrderBy(p => p.LastName).ThenBy(p => p.FirstName).ToListAsync(ct);
        return players.Select(Mappers.ToDto).ToList();
    }

    public async Task<ServiceResult<PlayerDto>> GetAsync(int id, CancellationToken ct)
    {
        var player = await WithCurrentTeam(db.Players.AsNoTracking()).FirstOrDefaultAsync(p => p.Id == id, ct);
        return player is null ? ServiceResult<PlayerDto>.NotFound() : ServiceResult<PlayerDto>.Ok(Mappers.ToDto(player));
    }

    // Every team the player has been on, newest first.
    public async Task<ServiceResult<IReadOnlyList<TeamPlayerDto>>> GetHistoryAsync(int id, CancellationToken ct)
    {
        if (!await db.Players.AnyAsync(p => p.Id == id, ct))
            return ServiceResult<IReadOnlyList<TeamPlayerDto>>.NotFound();

        var history = await db.TeamPlayers.AsNoTracking()
            .Include(tp => tp.Team).Include(tp => tp.Player)
            .Where(tp => tp.PlayerId == id)
            .OrderByDescending(tp => tp.StartDate)
            .ToListAsync(ct);
        return ServiceResult<IReadOnlyList<TeamPlayerDto>>.Ok(history.Select(Mappers.ToDto).ToList());
    }

    public async Task<ServiceResult<PlayerDto>> CreateAsync(PlayerRequest request, CancellationToken ct)
    {
        var player = new Player();
        Apply(player, request);
        db.Players.Add(player);
        await db.SaveChangesAsync(ct);
        return ServiceResult<PlayerDto>.Created(Mappers.ToDto(player));
    }

    public async Task<ServiceResult> UpdateAsync(int id, PlayerRequest request, CancellationToken ct)
    {
        var player = await db.Players.FindAsync([id], ct);
        if (player is null) return ServiceResult.NotFound();

        Apply(player, request);
        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken ct)
    {
        var player = await db.Players.FindAsync([id], ct);
        if (player is null) return ServiceResult.NotFound();

        if (await db.ScoringEvents.AnyAsync(s => s.PlayerId == id, ct))
            return ServiceResult.Conflict("Player has scoring events and cannot be deleted.");

        db.Players.Remove(player);
        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    private static IQueryable<Player> WithCurrentTeam(IQueryable<Player> query) =>
        query.Include(p => p.TeamPlayers.Where(tp => tp.EndDate == null)).ThenInclude(tp => tp.Team);

    private static void Apply(Player player, PlayerRequest r)
    {
        player.FirstName = r.FirstName;
        player.LastName = r.LastName;
        player.HeightInches = r.HeightInches;
        player.WeightLbs = r.WeightLbs;
        player.BirthDate = r.BirthDate;
    }
}
