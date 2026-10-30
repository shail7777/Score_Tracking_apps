using Microsoft.Extensions.Options;
using Nba.Api.Services;

namespace Nba.Api.Chat;

public record ChatMessageDto(string Role, string Content);
public record ChatRequest(List<ChatMessageDto>? Messages);
public record ChatReplyDto(string Reply);

public class FaqChatService(IChatModelClient model, IOptions<ChatOptions> options)
{
    private readonly ChatOptions _options = options.Value;
    private readonly Lazy<string> _systemPrompt = new(BuildSystemPrompt);

    public async Task<ServiceResult<ChatReplyDto>> AskAsync(ChatRequest request, CancellationToken ct)
    {
        var messages = request.Messages;
        if (messages is null || messages.Count == 0)
            return ServiceResult<ChatReplyDto>.BadRequest("At least one message is required.");
        if (messages.Any(m => m.Role is not ("user" or "assistant") || string.IsNullOrWhiteSpace(m.Content)))
            return ServiceResult<ChatReplyDto>.BadRequest("Messages need a role of 'user' or 'assistant' and some content.");
        if (messages[^1].Role != "user")
            return ServiceResult<ChatReplyDto>.BadRequest("The last message must come from the user.");
        if (messages.Any(m => m.Content.Length > _options.MaxMessageLength))
            return ServiceResult<ChatReplyDto>.BadRequest($"Messages can be at most {_options.MaxMessageLength} characters.");

        var turns = new List<ChatTurn> { new("system", _systemPrompt.Value) };
        turns.AddRange(messages.TakeLast(_options.MaxMessages).Select(m => new ChatTurn(m.Role, m.Content)));

        var reply = await model.CompleteAsync(turns, ct);
        if (reply.Length == 0) reply = "Sorry, I couldn't come up with an answer. Please try rephrasing.";
        return ServiceResult<ChatReplyDto>.Ok(new ChatReplyDto(reply));
    }

    private static string BuildSystemPrompt()
    {
        var faq = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Chat", "faq.md"));
        return $"""
            You are the help assistant for the NBA App, a web app for browsing NBA teams, players and games and keeping a play-by-play scorecard.
            Answer the user's questions using ONLY the FAQ below. Keep answers short and friendly (1-4 sentences).
            If the FAQ does not contain the answer, say you don't know and suggest checking the README or the Swagger page. Never invent features, buttons or settings.
            If the question is unrelated to the NBA App, politely say you can only help with questions about the app.

            FAQ:
            {faq}
            """;
    }
}
