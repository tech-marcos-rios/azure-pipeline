using PortfolioOps.Api.Models;
using PortfolioOps.Api.Services;

namespace PortfolioOps.Api.Endpoints;

public static class StatusEndpoints
{
    public static void MapStatusEndpoints(this WebApplication app)
    {
        app.MapGet("/api/status", async (IStatusAggregationService aggregationService, CancellationToken ct) =>
        {
            var status = await aggregationService.GetStatusAsync(ct);
            return Results.Ok(status);
        })
        .WithName("GetPortfolioStatus")
        .Produces<PortfolioStatus>();
    }
}
