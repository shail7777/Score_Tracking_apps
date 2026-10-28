using Microsoft.AspNetCore.Mvc;
using Nba.Api.Dtos;
using Nba.Api.Services;

namespace Nba.Api.Controllers;

[ApiController]
[Route("api/games/{gameId:int}")]
public class GameScoringController(ScoringService scoring) : ControllerBase
{
    // Records one basket (2 or 3 points) for a player and returns the updated scorecard.
    [HttpPost("events")]
    public async Task<ActionResult<ScorecardDto>> Record(int gameId, RecordEventRequest request, CancellationToken ct) =>
        ToResult(await scoring.RecordAsync(gameId, request.PlayerId, request.Points, ct, request.Quarter, request.ClockSeconds));

    [HttpPut("clock")]
    public async Task<ActionResult<ScorecardDto>> SetClock(int gameId, SetClockRequest request, CancellationToken ct) =>
        ToResult(await scoring.SetClockAsync(gameId, request.Quarter, request.ClockSeconds, ct));

    [HttpDelete("events/last")]
    public async Task<ActionResult<ScorecardDto>> UndoLast(int gameId, CancellationToken ct) =>
        ToResult(await scoring.UndoLastAsync(gameId, ct));

    [HttpPost("finish")]
    public async Task<ActionResult<ScorecardDto>> Finish(int gameId, CancellationToken ct) =>
        ToResult(await scoring.FinishAsync(gameId, ct));

    private ActionResult<ScorecardDto> ToResult(ScoringOutcome outcome) => outcome.StatusCode switch
    {
        200 => outcome.Scorecard!,
        404 => NotFound(new ProblemDetails { Title = outcome.Error, Status = 404 }),
        409 => Conflict(new ProblemDetails { Title = outcome.Error, Status = 409 }),
        _ => BadRequest(new ProblemDetails { Title = outcome.Error, Status = 400 }),
    };
}
