using Microsoft.AspNetCore.Mvc;
using Nba.Api.Chat;
using Nba.Api.Services;

namespace Nba.Api.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController(FaqChatService chat) : ApiControllerBase
{
    // Answers an FAQ question using the local Ollama model. 503 when Ollama isn't reachable.
    [HttpPost]
    public async Task<ActionResult<ChatReplyDto>> Ask(ChatRequest request, CancellationToken ct)
    {
        try
        {
            return Respond(await chat.AskAsync(request, ct));
        }
        catch (ChatUnavailableException ex)
        {
            return StatusCode(503, new ProblemDetails { Title = ex.Message, Status = 503 });
        }
    }
}
