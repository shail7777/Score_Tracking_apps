using Microsoft.AspNetCore.Mvc;
using Nba.Api.Dtos;
using Nba.Api.Services;

namespace Nba.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlayersController(IPlayerService players) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PlayerDto>>> GetAll(
        [FromQuery] int? teamId, [FromQuery] string? search, CancellationToken ct) =>
        Ok(await players.GetAllAsync(teamId, search, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PlayerDto>> Get(int id, CancellationToken ct) =>
        Respond(await players.GetAsync(id, ct));

    // Every team the player has been on, newest first.
    [HttpGet("{id:int}/history")]
    public async Task<ActionResult<IEnumerable<TeamPlayerDto>>> GetHistory(int id, CancellationToken ct)
    {
        var result = await players.GetHistoryAsync(id, ct);
        return result.Succeeded ? Ok(result.Value) : Failure(result.Status, result.Error);
    }

    [HttpPost]
    public async Task<ActionResult<PlayerDto>> Create(PlayerRequest request, CancellationToken ct)
    {
        var result = await players.CreateAsync(request, ct);
        if (!result.Succeeded) return Failure(result.Status, result.Error);
        return CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, PlayerRequest request, CancellationToken ct) =>
        Respond(await players.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        Respond(await players.DeleteAsync(id, ct));
}
