using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Nba.Api.Import;

public record ApiPlayer(string? FirstName, string? LastName);

public record ApiTeam(string? ShortName, string? NickName, List<ApiPlayer>? Players);

public record ApiGame(string? Date, string? LocalDate, ApiTeam? HomeTeam, ApiTeam? VisitorTeam);

public class NbaGraphQlClient(HttpClient http, IOptions<NbaGraphQlOptions> options)
{
    private const string GamesQuery = """
        query Games($leagueId: ID, $dateFilter: DateFilterInput, $options: ListOptionsInput) {
          games(leagueId: $leagueId, dateFilter: $dateFilter, options: $options) {
            date
            localDate
            homeTeam { shortName nickName players { firstName lastName } }
            visitorTeam { shortName nickName players { firstName lastName } }
          }
        }
        """;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<List<ApiGame>> GetGamesAsync(CancellationToken ct)
    {
        var o = options.Value;
        var body = JsonSerializer.Serialize(new
        {
            query = GamesQuery,
            variables = new
            {
                leagueId = o.LeagueId,
                dateFilter = new { startDate = o.StartDate, endDate = o.EndDate },
                options = new { limit = o.Limit },
            },
        }, Json);

        using var request = new HttpRequestMessage(HttpMethod.Post, o.Url)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add(o.ApiKeyHeader, o.ApiKey);
        if (!string.IsNullOrWhiteSpace(o.ClientId))
            request.Headers.Add(o.ClientIdHeader, o.ClientId);

        using var response = await http.SendAsync(request, ct);
        var payload = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"GraphQL request failed: {(int)response.StatusCode} {payload}");

        var result = JsonSerializer.Deserialize<GraphQlResponse>(payload, Json)
            ?? throw new InvalidOperationException("Empty GraphQL response.");
        if (result.Errors is { Count: > 0 })
            throw new InvalidOperationException("GraphQL errors: " + string.Join("; ", result.Errors.Select(e => e.Message)));

        return result.Data?.Games ?? [];
    }

    private record GraphQlResponse(GraphQlData? Data, List<GraphQlError>? Errors);

    private record GraphQlData([property: JsonPropertyName("games")] List<ApiGame>? Games);

    private record GraphQlError(string? Message);
}
