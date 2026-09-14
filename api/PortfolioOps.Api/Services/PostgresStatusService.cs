using Npgsql;
using PortfolioOps.Api.Models;

namespace PortfolioOps.Api.Services;

public sealed class PostgresStatusService(IDockerStatusService dockerStatusService, ILogger<PostgresStatusService> logger)
    : IDatabaseStatusService
{
    public async Task<DatabaseStatus> GetStatusAsync(
        string dockerNetwork,
        string dbContainer,
        string dbName,
        string dbUser,
        string? password,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(password))
            return new DatabaseStatus(Reachable: false, SizeBytes: null, ActiveConnections: null, Error: "No password configured");

        var ip = await dockerStatusService.GetContainerIpAddressAsync(dbContainer, dockerNetwork, ct);
        if (ip is null)
            return new DatabaseStatus(Reachable: false, SizeBytes: null, ActiveConnections: null, Error: "Container not reachable on Docker network");

        var connectionString = new NpgsqlConnectionStringBuilder
        {
            Host = ip,
            Port = 5432,
            Database = dbName,
            Username = dbUser,
            Password = password,
            Timeout = 3,
            CommandTimeout = 3
        }.ToString();

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var command = new NpgsqlCommand(
                """
                SELECT pg_database_size(current_database()),
                       (SELECT count(*) FROM pg_stat_activity WHERE datname = current_database())
                """, connection);

            await using var reader = await command.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                return new DatabaseStatus(Reachable: true, SizeBytes: null, ActiveConnections: null, Error: "Empty result");

            var sizeBytes = reader.GetInt64(0);
            var activeConnections = reader.GetInt32(1);
            return new DatabaseStatus(Reachable: true, sizeBytes, activeConnections, Error: null);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Postgres status check failed for {DbContainer}", dbContainer);
            return new DatabaseStatus(Reachable: false, SizeBytes: null, ActiveConnections: null, Error: ex.Message);
        }
    }
}
