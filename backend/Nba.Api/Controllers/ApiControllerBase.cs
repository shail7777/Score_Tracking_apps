using Microsoft.AspNetCore.Mvc;
using Nba.Api.Services;

namespace Nba.Api.Controllers;

// Translates service results into HTTP responses so controllers stay free of business logic.
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult Failure(ServiceStatus status, string? error) => status switch
    {
        ServiceStatus.NotFound => NotFound(),
        ServiceStatus.BadRequest => BadRequest(new ProblemDetails { Title = error }),
        ServiceStatus.Conflict => Conflict(new ProblemDetails { Title = error }),
        _ => throw new InvalidOperationException($"Unexpected failure status {status}."),
    };

    // 200 with the value, or the matching error response.
    protected ActionResult<T> Respond<T>(ServiceResult<T> result)
    {
        if (!result.Succeeded) return Failure(result.Status, result.Error);
        return Ok(result.Value);
    }

    // 204 for success (update/delete), or the matching error response.
    protected IActionResult Respond(ServiceResult result)
    {
        if (!result.Succeeded) return Failure(result.Status, result.Error);
        return NoContent();
    }
}
