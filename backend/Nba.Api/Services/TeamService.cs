using Microsoft.EntityFrameworkCore;
using Nba.Api.Data;
using Nba.Api.Dtos;
using Nba.Api.Models;

namespace Nba.Api.Services;

public interface ITeamService
{
    Task<IReadOnlyList<TeamDto>> GetAllAsync(CancellationToken ct);
    Task<ServiceResult<TeamDto>> GetAsync(int id, CancellationToken ct);
    Task<ServiceResult<IReadOnlyList<TeamPlayerDto>>> GetRosterAsync(int id, bool activeOnly, CancellationToken ct);
    Task<ServiceResult<IReadOnlyList<TeamGameDto>>> GetScheduleAsync(int id, GameStatus? status, CancellationToken ct);
    Task<ServiceResult<TeamDto>> CreateAsync(TeamRequest request, CancellationToken ct);
    Task<ServiceResult> UpdateAsync(int id, TeamRequest request, CancellationToken ct);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken ct);
}

public class TeamService(NbaDbContext db) : ITeamService
{
    public async Task<IReadOnlyList<TeamDto>> GetAllAsync(CancellationToken ct)
    {
        var teams = await db.Teams.AsNoTracking().OrderBy(t => t.Name).ToListAsync(ct);
        return teams.Select(Mappers.ToDto).ToList();
    }

    public async Task<ServiceResult<TeamDto>> GetAsync(int id, CancellationToken ct)
    {
        var team = await db.Teams.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);
        return team is null ? ServiceResult<TeamDto>.NotFound() : ServiceResult<TeamDto>.Ok(Mappers.ToDto(team));
    }

    // Current roster by default; activeOnly=false includes former players.
    public async Task<ServiceResult<IReadOnlyList<TeamPlayerDto>>> GetRosterAsync(
        int id, bool activeOnly, CancellationToken ct)
    {
        if (!await db.Teams.AnyAsync(t => t.Id == id, ct))
            return ServiceResult<IReadOnlyList<TeamPlayerDto>>.NotFound();

        var query = db.TeamPlayers.AsNoTracking()
            .Include(tp => tp.Team).Include(tp => tp.Player)
            .Where(tp => tp.TeamId == id);
        if (activeOnly) query = query.Where(tp => tp.EndDate == null);

        var roster = await query.OrderBy(tp => tp.Player!.LastName).ToListAsync(ct);
        return ServiceResult<IReadOnlyList<TeamPlayerDto>>.Ok(roster.Select(Mappers.ToDto).ToList());
    }

    public async Task<ServiceResult<IReadOnlyList<TeamGameDto>>> GetScheduleAsync(
        int id, GameStatus? status, CancellationToken ct)
    {
        if (!await db.Teams.AnyAsync(t => t.Id == id, ct))
            return ServiceResult<IReadOnlyList<TeamGameDto>>.NotFound();

        var query = db.Games.AsNoTracking()
            .Include(g => g.HomeTeam).Include(g => g.AwayTeam)
            .Where(g => g.HomeTeamId == id || g.AwayTeamId == id);
        if (status is not null) query = query.Where(g => g.Status == status);

        // Schedule order: earliest first.
        var games = await query.OrderBy(g => g.GameDate).ThenBy(g => g.Id).ToListAsync(ct);
        return ServiceResult<IReadOnlyList<TeamGameDto>>.Ok(games.Select(g => Mappers.ToTeamGameDto(g, id)).ToList());
    }

    public async Task<ServiceResult<TeamDto>> CreateAsync(TeamRequest request, CancellationToken ct)
    {
        var team = new Team();
        Apply(team, request);
        db.Teams.Add(team);
        await db.SaveChangesAsync(ct);
        return ServiceResult<TeamDto>.Created(Mappers.ToDto(team));
    }

    public async Task<ServiceResult> UpdateAsync(int id, TeamRequest request, CancellationToken ct)
    {
        var team = await db.Teams.FindAsync([id], ct);
        if (team is null) return ServiceResult.NotFound();

        Apply(team, request);
        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken ct)
    {
        var team = await db.Teams.FindAsync([id], ct);
        if (team is null) return ServiceResult.NotFound();

        if (await db.Games.AnyAsync(g => g.HomeTeamId == id || g.AwayTeamId == id, ct))
            return ServiceResult.Conflict("Team has games and cannot be deleted.");

        db.Teams.Remove(team);
        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    private static void Apply(Team team, TeamRequest r)
    {
        team.Name = r.Name;
        team.City = r.City;
        team.Conference = r.Conference;
        team.Division = r.Division;
    }
}
