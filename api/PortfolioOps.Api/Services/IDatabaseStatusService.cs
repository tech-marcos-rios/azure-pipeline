using PortfolioOps.Api.Models;

namespace PortfolioOps.Api.Services;

public interface IDatabaseStatusService
{
    Task<DatabaseStatus> GetStatusAsync(
        string dockerNetwork,
        string dbContainer,
        string dbName,
        string dbUser,
        string? password,
        CancellationToken ct = default);
}
