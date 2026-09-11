using PortfolioOps.Api.Configuration;
using PortfolioOps.Api.Models;

namespace PortfolioOps.Api.Services;

public interface IDatabaseStatusService
{
    Task<DatabaseStatus> GetStatusAsync(MonitoredProjectOptions project, string? password, CancellationToken ct = default);
}
