using Microsoft.Extensions.Options;
using PortfolioOps.Api.Configuration;
using PortfolioOps.Api.Models;

namespace PortfolioOps.Api.Services;

public interface IStatusAggregationService
{
    Task<PortfolioStatus> GetStatusAsync(CancellationToken ct = default);
}

public sealed class StatusAggregationService(
    IOptions<List<MonitoredProjectOptions>> monitoredProjects,
    IOptions<ProjectSecretsOptions> secrets,
    IHealthCheckService healthCheckService,
    IDockerStatusService dockerStatusService,
    IDatabaseStatusService databaseStatusService,
    IDiskStatusService diskStatusService) : IStatusAggregationService
{
    public async Task<PortfolioStatus> GetStatusAsync(CancellationToken ct = default)
    {
        var projectTasks = monitoredProjects.Value.Select(project => GetProjectStatusAsync(project, ct));
        var projects = await Task.WhenAll(projectTasks);
        var disk = diskStatusService.GetStatus();

        return new PortfolioStatus(DateTimeOffset.UtcNow, disk, projects);
    }

    private async Task<ProjectStatus> GetProjectStatusAsync(MonitoredProjectOptions project, CancellationToken ct)
    {
        secrets.Value.DbPasswords.TryGetValue(project.Id, out var password);

        var healthTask = healthCheckService.CheckAsync(project.HealthUrl, ct);
        var apiContainerTask = dockerStatusService.GetContainerStatusAsync(project.ApiContainer, ct);
        var dbContainerTask = dockerStatusService.GetContainerStatusAsync(project.DbContainer, ct);
        var databaseTask = databaseStatusService.GetStatusAsync(project, password, ct);

        await Task.WhenAll(healthTask, apiContainerTask, dbContainerTask, databaseTask);

        return new ProjectStatus(
            project.Name,
            await healthTask,
            await apiContainerTask,
            await dbContainerTask,
            await databaseTask);
    }
}
