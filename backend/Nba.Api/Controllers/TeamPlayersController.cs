using Microsoft.AspNetCore.Mvc;
using Nba.Api.Dtos;
using Nba.Api.Services;

namespace Nba.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeamPlayersController(ITeamPlayerService teamPlayers) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TeamPlayerDto>>> GetAll(
        [FromQuery] int? teamId, [FromQuery] int? playerId, [FromQuery] bool activeOnly = false,
        CancellationToken ct = default) =>
        Ok(await teamPlayers.GetAllAsync(teamId, playerId, activeOnly, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TeamPlayerDto>> Get(int id, CancellationToken ct) =>
        Respond(await teamPlayers.GetAsync(id, ct));

    // Adding an active stint ends the player's other active stint (a trade/signing).
    [HttpPost]
    public async Task<ActionResult<TeamPlayerDto>> Create(TeamPlayerRequest request, CancellationToken ct)
    {
        var result = await teamPlayers.CreateAsync(request, ct);
        if (!result.Succeeded) return Failure(result.Status, result.Error);
        return CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, TeamPlayerRequest request, CancellationToken ct) =>
        Respond(await teamPlayers.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        Respond(await teamPlayers.DeleteAsync(id, ct));
}
