using Microsoft.AspNetCore.Mvc;
using Nba.Api.Dtos;
using Nba.Api.Models;
using Nba.Api.Services;

namespace Nba.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GamesController(IGameService games) : ApiControllerBase
{
    // status=Final returns previous games (newest first); status=Scheduled returns upcoming (soonest first).
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GameDto>>> GetAll(
        [FromQuery] int? teamId, [FromQuery] GameStatus? status, [FromQuery] string? season,
        CancellationToken ct) =>
        Ok(await games.GetAllAsync(teamId, status, season, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GameDto>> Get(int id, CancellationToken ct) =>
        Respond(await games.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<GameDto>> Create(GameRequest request, CancellationToken ct)
    {
        var result = await games.CreateAsync(request, ct);
        if (!result.Succeeded) return Failure(result.Status, result.Error);
        return CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, GameRequest request, CancellationToken ct) =>
        Respond(await games.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        Respond(await games.DeleteAsync(id, ct));
}
