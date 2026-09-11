namespace PortfolioOps.Api.Configuration;

// Passwords de las bases de cada proyecto monitoreado, por nombre. Nunca en
// appsettings.json — se cargan desde variables de entorno en el .env del
// server (mismo patrón que el resto del portfolio: credenciales propias del
// proyecto nunca como secret de GitHub, solo en el .env con chmod 600).
public sealed class ProjectSecretsOptions
{
    public Dictionary<string, string> DbPasswords { get; init; } = new();
}
