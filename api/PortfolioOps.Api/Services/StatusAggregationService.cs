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

        var hasDatabase = project.DockerNetwork is not null && project.DbContainer is not null
            && project.DbName is not null && project.DbUser is not null;

        var healthTask = project.HealthUrl is not null
            ? RunAsync(() => healthCheckService.CheckAsync(project.HealthUrl, ct))
            : Task.FromResult<HealthCheckResult?>(null);

        var apiContainerTask = dockerStatusService.GetContainerStatusAsync(project.ApiContainer, ct);

        var dbContainerTask = project.DbContainer is not null
            ? RunAsync(() => dockerStatusService.GetContainerStatusAsync(project.DbContainer, ct))
            : Task.FromResult<ContainerStatus?>(null);

        var databaseTask = hasDatabase
            ? RunAsync(() => databaseStatusService.GetStatusAsync(project.DockerNetwork!, project.DbContainer!, project.DbName!, project.DbUser!, password, ct))
            : Task.FromResult<DatabaseStatus?>(null);

        await Task.WhenAll(healthTask, apiContainerTask, dbContainerTask, databaseTask);

        return new ProjectStatus(
            project.Name,
            project.Url,
            await healthTask,
            await apiContainerTask,
            await dbContainerTask,
            await databaseTask);
    }

    // Envuelve una Task<T> en una Task<T?> — Task<T> no es covariante con
    // Task<T?>, así que no se puede devolver directo donde el ternario de
    // arriba espera el mismo tipo que la rama `null`.
    private static async Task<T?> RunAsync<T>(Func<Task<T>> action) => await action();
}
