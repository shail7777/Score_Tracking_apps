namespace Nba.Api.Chat;

public class ChatOptions
{
    // Local Ollama server; nothing is sent to a remote service.
    public string OllamaUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "llama3.2:3b";
    public int TimeoutSeconds { get; set; } = 120;
    public int MaxMessages { get; set; } = 20;
    public int MaxMessageLength { get; set; } = 1000;
}
