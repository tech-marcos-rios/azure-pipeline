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
    // Adónde lleva el nombre del proyecto en la UI — la app en vivo si tiene
    // una (ej. https://ministock.marcosrios.dev), o el repo de GitHub si no
    // expone nada público (worker de fondo sin puertos, como market-mind).
    public required string Url { get; init; }
    public string? HealthUrl { get; init; }
    public required string ApiContainer { get; init; }
    // Los cuatro campos de abajo solo aplican a proyectos con Postgres
    // propio — null para un worker sin base de datos.
    public string? DockerNetwork { get; init; }
    public string? DbContainer { get; init; }
    public string? DbName { get; init; }
    public string? DbUser { get; init; }
}
