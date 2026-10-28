using Microsoft.AspNetCore.Mvc;
using Nba.Api.Dtos;
using Nba.Api.Models;
using Nba.Api.Services;

namespace Nba.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeamsController(ITeamService teams) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TeamDto>>> GetAll(CancellationToken ct) =>
        Ok(await teams.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TeamDto>> Get(int id, CancellationToken ct) =>
        Respond(await teams.GetAsync(id, ct));

    // Current roster by default; pass activeOnly=false to include former players.
    [HttpGet("{id:int}/roster")]
    public async Task<ActionResult<IEnumerable<TeamPlayerDto>>> GetRoster(
        int id, [FromQuery] bool activeOnly = true, CancellationToken ct = default)
    {
        var result = await teams.GetRosterAsync(id, activeOnly, ct);
        return result.Succeeded ? Ok(result.Value) : Failure(result.Status, result.Error);
    }

    [HttpGet("{id:int}/games")]
    public async Task<ActionResult<IEnumerable<TeamGameDto>>> GetGames(
        int id, [FromQuery] GameStatus? status, CancellationToken ct)
    {
        var result = await teams.GetScheduleAsync(id, status, ct);
        return result.Succeeded ? Ok(result.Value) : Failure(result.Status, result.Error);
    }

    [HttpPost]
    public async Task<ActionResult<TeamDto>> Create(TeamRequest request, CancellationToken ct)
    {
        var result = await teams.CreateAsync(request, ct);
        if (!result.Succeeded) return Failure(result.Status, result.Error);
        return CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, TeamRequest request, CancellationToken ct) =>
        Respond(await teams.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        Respond(await teams.DeleteAsync(id, ct));
}
