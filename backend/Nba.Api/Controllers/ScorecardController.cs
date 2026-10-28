using Microsoft.AspNetCore.Mvc;
using Nba.Api.Dtos;
using Nba.Api.Services;

namespace Nba.Api.Controllers;

[ApiController]
[Route("api/games/{gameId:int}/scorecard")]
public class ScorecardController(ScorecardService scorecards) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ScorecardDto>> Get(int gameId, CancellationToken ct)
    {
        var scorecard = await scorecards.BuildAsync(gameId, ct);
        return scorecard is null ? NotFound() : scorecard;
    }
}
