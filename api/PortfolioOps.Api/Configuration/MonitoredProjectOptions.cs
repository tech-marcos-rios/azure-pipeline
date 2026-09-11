namespace PortfolioOps.Api.Configuration;

// Un proyecto del portfolio a monitorear. Se enlaza desde la sección
// "MonitoredProjects" de appsettings.json — los datos no sensibles (nombres
// de contenedor, red, base) viven ahí; el password de cada base se resuelve
// aparte, por nombre de proyecto, desde variables de entorno (ver
// ProjectSecretsOptions) para no comprometer credenciales en el repo.
public sealed class MonitoredProjectOptions
{
    // Slug corto (kebab-case, sin espacios) — clave para el diccionario de
    // passwords en ProjectSecretsOptions, que se llena desde variables de
    // entorno (los nombres de env var no admiten espacios).
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string HealthUrl { get; init; }
    public required string DockerNetwork { get; init; }
    public required string ApiContainer { get; init; }
    public required string DbContainer { get; init; }
    public required string DbName { get; init; }
    public required string DbUser { get; init; }
}
