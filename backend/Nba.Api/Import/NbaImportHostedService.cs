using Microsoft.Extensions.Options;

namespace Nba.Api.Import;

// Runs one import after the app has started, so a slow or failing upstream API never blocks startup.
public class NbaImportHostedService(
    IServiceScopeFactory scopes, IOptions<NbaGraphQlOptions> options, ILogger<NbaImportHostedService> log)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var o = options.Value;
        if (!o.ImportOnStartup || string.IsNullOrWhiteSpace(o.Url) || string.IsNullOrWhiteSpace(o.ApiKey))
        {
            log.LogInformation("NBA import skipped (disabled, or NbaGraphQl:Url / NbaGraphQl:ApiKey not set).");
            return;
        }

        try
        {
            await using var scope = scopes.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<NbaImportService>().ImportAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            log.LogError(ex, "NBA import failed.");
        }
    }
}
