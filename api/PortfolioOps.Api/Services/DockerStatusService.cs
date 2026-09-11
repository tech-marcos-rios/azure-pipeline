using Docker.DotNet;
using PortfolioOps.Api.Models;

namespace PortfolioOps.Api.Services;

public sealed class DockerStatusService(IDockerClient dockerClient, ILogger<DockerStatusService> logger)
    : IDockerStatusService
{
    public async Task<ContainerStatus> GetContainerStatusAsync(string containerName, CancellationToken ct = default)
    {
        try
        {
            var inspect = await dockerClient.Containers.InspectContainerAsync(containerName, ct);
            var startedAt = DateTimeOffset.TryParse(inspect.State.StartedAt, out var parsed) ? parsed : (DateTimeOffset?)null;
            return new ContainerStatus(containerName, Found: true, inspect.State.Status, startedAt, Error: null);
        }
        catch (DockerContainerNotFoundException)
        {
            return new ContainerStatus(containerName, Found: false, State: null, StartedAt: null, Error: "Container not found");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Docker inspect failed for {ContainerName}", containerName);
            return new ContainerStatus(containerName, Found: false, State: null, StartedAt: null, Error: ex.Message);
        }
    }

    public async Task<string?> GetContainerIpAddressAsync(string containerName, string networkName, CancellationToken ct = default)
    {
        try
        {
            var inspect = await dockerClient.Containers.InspectContainerAsync(containerName, ct);
            return inspect.NetworkSettings.Networks.TryGetValue(networkName, out var network)
                ? network.IPAddress
                : null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not resolve IP for {ContainerName} on {NetworkName}", containerName, networkName);
            return null;
        }
    }
}
