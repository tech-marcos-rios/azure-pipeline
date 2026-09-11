using PortfolioOps.Api.Models;

namespace PortfolioOps.Api.Services;

// En producción el filesystem del host se monta read-only dentro del
// contenedor (ver deploy/docker-compose.yml, "/:/host:ro") porque DriveInfo
// dentro de un contenedor solo ve el disco del propio contenedor, no el
// disco real del server. En local (sin ese mount) cae al drive de la app.
public sealed class DiskStatusService(IConfiguration configuration, ILogger<DiskStatusService> logger) : IDiskStatusService
{
    public DiskStatus GetStatus()
    {
        var configuredPath = configuration["HostDiskPath"];
        var path = !string.IsNullOrEmpty(configuredPath) && Directory.Exists(configuredPath)
            ? configuredPath
            : Path.GetPathRoot(AppContext.BaseDirectory) ?? "/";

        try
        {
            var drive = new DriveInfo(path);
            var used = drive.TotalSize - drive.AvailableFreeSpace;
            var usedPercent = drive.TotalSize == 0 ? 0 : Math.Round(used * 100.0 / drive.TotalSize, 1);
            return new DiskStatus(drive.TotalSize, drive.AvailableFreeSpace, usedPercent);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not read disk status for {Path}", path);
            return new DiskStatus(0, 0, 0, ex.Message);
        }
    }
}
