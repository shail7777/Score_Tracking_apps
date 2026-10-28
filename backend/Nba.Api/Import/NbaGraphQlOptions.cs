namespace Nba.Api.Import;

public class NbaGraphQlOptions
{
    public string Url { get; set; } = string.Empty;
    public string ApiKeyHeader { get; set; } = "client-api-key";
    // Supply via environment variable (NbaGraphQl__ApiKey) or user-secrets; never commit it.
    public string? ApiKey { get; set; }
    public string ClientIdHeader { get; set; } = "client-id";
    // Sent only when set; supply via NbaGraphQl__ClientId or user-secrets.
    public string? ClientId { get; set; }
    public string LeagueId { get; set; } = "00";
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public int Limit { get; set; } = 200;
    public bool ImportOnStartup { get; set; } = true;
}
