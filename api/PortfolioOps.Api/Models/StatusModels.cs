namespace PortfolioOps.Api.Models;

public enum HealthState
{
    Healthy,
    Degraded,
    Unhealthy,
    Unknown
}

public sealed record HealthCheckResult(HealthState State, int? StatusCode, long? ResponseTimeMs, string? Error);

public sealed record ContainerStatus(string Name, bool Found, string? State, DateTimeOffset? StartedAt, string? Error);

public sealed record DatabaseStatus(bool Reachable, long? SizeBytes, int? ActiveConnections, string? Error);

public sealed record DiskStatus(long TotalBytes, long FreeBytes, double UsedPercent, string? Error = null);

public sealed record ProjectStatus(
    string Name,
    string Url,
    // Null cuando el proyecto no expone health HTTP / no tiene contenedor de
    // base separado (p. ej. un worker de fondo sin puertos) — no es lo mismo
    // que HealthState.Unknown, que significa "se chequeó y no se entendió el
    // resultado". Acá directamente no aplica.
    HealthCheckResult? Health,
    ContainerStatus ApiContainer,
    ContainerStatus? DbContainer,
    DatabaseStatus? Database);

public sealed record PortfolioStatus(DateTimeOffset CheckedAt, DiskStatus Disk, IReadOnlyList<ProjectStatus> Projects);
