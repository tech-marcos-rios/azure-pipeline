using PortfolioOps.Api.Models;

namespace PortfolioOps.Api.Services;

public interface IDockerStatusService
{
    Task<ContainerStatus> GetContainerStatusAsync(string containerName, CancellationToken ct = default);

    // IP del contenedor dentro de una red Docker puntual — se usa para
    // conectar directo a la base de un proyecto sin depender de la
    // resolución DNS embebida de Docker (ambigua cuando el contenedor de
    // ops está conectado a varias redes que repiten el mismo nombre de
    // servicio, p. ej. "db" en las tres redes de 02/04/05).
    Task<string?> GetContainerIpAddressAsync(string containerName, string networkName, CancellationToken ct = default);
}
