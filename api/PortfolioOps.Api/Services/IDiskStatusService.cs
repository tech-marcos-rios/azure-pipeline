using PortfolioOps.Api.Models;

namespace PortfolioOps.Api.Services;

public interface IDiskStatusService
{
    DiskStatus GetStatus();
}
