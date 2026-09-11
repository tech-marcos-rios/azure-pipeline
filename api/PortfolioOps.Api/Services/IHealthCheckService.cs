using PortfolioOps.Api.Models;

namespace PortfolioOps.Api.Services;

public interface IHealthCheckService
{
    Task<HealthCheckResult> CheckAsync(string healthUrl, CancellationToken ct = default);
}
