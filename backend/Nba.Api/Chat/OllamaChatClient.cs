using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Nba.Api.Chat;

public record ChatTurn(string Role, string Content);

public class ChatUnavailableException(string message, Exception? inner = null) : Exception(message, inner);

public interface IChatModelClient
{
    Task<string> CompleteAsync(IReadOnlyList<ChatTurn> messages, CancellationToken ct);
}

public class OllamaChatClient(HttpClient http, IOptions<ChatOptions> options) : IChatModelClient
{
    private readonly ChatOptions _options = options.Value;

    public async Task<string> CompleteAsync(IReadOnlyList<ChatTurn> messages, CancellationToken ct)
    {
        var request = new OllamaRequest(
            _options.Model,
            messages.Select(m => new OllamaMessage(m.Role, m.Content)).ToList(),
            false,
            new OllamaModelOptions(0.2));

        try
        {
            using var response = await http.PostAsJsonAsync("/api/chat", request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var detail = await response.Content.ReadAsStringAsync(ct);
                throw new ChatUnavailableException(
                    $"Ollama returned {(int)response.StatusCode}. Is the model '{_options.Model}' pulled? {detail}");
            }

            var body = await response.Content.ReadFromJsonAsync<OllamaResponse>(ct);
            return body?.Message?.Content?.Trim() ?? string.Empty;
        }
        catch (HttpRequestException ex)
        {
            throw new ChatUnavailableException("Could not reach Ollama. Is it running?", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new ChatUnavailableException("Ollama took too long to respond.", ex);
        }
    }

    private record OllamaMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private record OllamaModelOptions([property: JsonPropertyName("temperature")] double Temperature);

    private record OllamaRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] List<OllamaMessage> Messages,
        [property: JsonPropertyName("stream")] bool Stream,
        [property: JsonPropertyName("options")] OllamaModelOptions Options);

    private record OllamaResponse([property: JsonPropertyName("message")] OllamaMessage? Message);
}
