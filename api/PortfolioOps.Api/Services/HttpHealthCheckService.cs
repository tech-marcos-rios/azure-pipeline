using System.Diagnostics;
using PortfolioOps.Api.Models;

namespace PortfolioOps.Api.Services;

public sealed class HttpHealthCheckService(IHttpClientFactory httpClientFactory, ILogger<HttpHealthCheckService> logger)
    : IHealthCheckService
{
    public async Task<HealthCheckResult> CheckAsync(string healthUrl, CancellationToken ct = default)
    {
        var client = httpClientFactory.CreateClient(nameof(HttpHealthCheckService));
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var response = await client.GetAsync(healthUrl, ct);
            stopwatch.Stop();

            var state = response.IsSuccessStatusCode ? HealthState.Healthy : HealthState.Degraded;
            return new HealthCheckResult(state, (int)response.StatusCode, stopwatch.ElapsedMilliseconds, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            stopwatch.Stop();
            logger.LogWarning(ex, "Health check failed for {HealthUrl}", healthUrl);
            return new HealthCheckResult(HealthState.Unhealthy, null, stopwatch.ElapsedMilliseconds, ex.Message);
        }
    }
}
