using Microsoft.EntityFrameworkCore;
using Nba.Api.Data;
using Nba.Api.Dtos;
using Nba.Api.Models;

namespace Nba.Api.Services;

public interface ITeamPlayerService
{
    Task<IReadOnlyList<TeamPlayerDto>> GetAllAsync(int? teamId, int? playerId, bool activeOnly, CancellationToken ct);
    Task<ServiceResult<TeamPlayerDto>> GetAsync(int id, CancellationToken ct);
    Task<ServiceResult<TeamPlayerDto>> CreateAsync(TeamPlayerRequest request, CancellationToken ct);
    Task<ServiceResult> UpdateAsync(int id, TeamPlayerRequest request, CancellationToken ct);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken ct);
}

public class TeamPlayerService(NbaDbContext db) : ITeamPlayerService
{
    public async Task<IReadOnlyList<TeamPlayerDto>> GetAllAsync(
        int? teamId, int? playerId, bool activeOnly, CancellationToken ct)
    {
        var query = db.TeamPlayers.AsNoTracking().Include(tp => tp.Team).Include(tp => tp.Player).AsQueryable();
        if (teamId is not null) query = query.Where(tp => tp.TeamId == teamId);
        if (playerId is not null) query = query.Where(tp => tp.PlayerId == playerId);
        if (activeOnly) query = query.Where(tp => tp.EndDate == null);

        var items = await query.OrderByDescending(tp => tp.StartDate).ToListAsync(ct);
        return items.Select(Mappers.ToDto).ToList();
    }

    public async Task<ServiceResult<TeamPlayerDto>> GetAsync(int id, CancellationToken ct)
    {
        var item = await db.TeamPlayers.AsNoTracking()
            .Include(tp => tp.Team).Include(tp => tp.Player)
            .FirstOrDefaultAsync(tp => tp.Id == id, ct);
        return item is null ? ServiceResult<TeamPlayerDto>.NotFound() : ServiceResult<TeamPlayerDto>.Ok(Mappers.ToDto(item));
    }

    // Adding an active stint ends the player's other active stint (a trade/signing).
    public async Task<ServiceResult<TeamPlayerDto>> CreateAsync(TeamPlayerRequest request, CancellationToken ct)
    {
        if (await Validate(request, ct) is { } error) return ServiceResult<TeamPlayerDto>.BadRequest(error);

        if (request.EndDate is null)
        {
            var open = await db.TeamPlayers
                .Where(tp => tp.PlayerId == request.PlayerId && tp.EndDate == null)
                .ToListAsync(ct);
            foreach (var tp in open) tp.EndDate = request.StartDate;
        }

        var item = new TeamPlayer();
        Apply(item, request);
        db.TeamPlayers.Add(item);
        await db.SaveChangesAsync(ct);

        await db.Entry(item).Reference(tp => tp.Team).LoadAsync(ct);
        await db.Entry(item).Reference(tp => tp.Player).LoadAsync(ct);
        return ServiceResult<TeamPlayerDto>.Created(Mappers.ToDto(item));
    }

    public async Task<ServiceResult> UpdateAsync(int id, TeamPlayerRequest request, CancellationToken ct)
    {
        var item = await db.TeamPlayers.FindAsync([id], ct);
        if (item is null) return ServiceResult.NotFound();
        if (await Validate(request, ct) is { } error) return ServiceResult.BadRequest(error);

        Apply(item, request);
        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken ct)
    {
        var item = await db.TeamPlayers.FindAsync([id], ct);
        if (item is null) return ServiceResult.NotFound();

        db.TeamPlayers.Remove(item);
        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    // Returns an error message, or null when the request is valid.
    private async Task<string?> Validate(TeamPlayerRequest r, CancellationToken ct)
    {
        if (!await db.Teams.AnyAsync(t => t.Id == r.TeamId, ct)) return $"Team {r.TeamId} does not exist.";
        if (!await db.Players.AnyAsync(p => p.Id == r.PlayerId, ct)) return $"Player {r.PlayerId} does not exist.";
        if (r.EndDate is not null && r.EndDate < r.StartDate) return "EndDate cannot be before StartDate.";
        return null;
    }

    private static void Apply(TeamPlayer item, TeamPlayerRequest r)
    {
        item.TeamId = r.TeamId;
        item.PlayerId = r.PlayerId;
        item.StartDate = r.StartDate;
        item.EndDate = r.EndDate;
    }
}
