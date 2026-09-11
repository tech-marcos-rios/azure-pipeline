import type { ContainerStatus, DatabaseStatus, ProjectStatus } from "@/lib/types";
import { formatBytes, formatUptime } from "@/lib/format";
import { StatusBadge } from "./StatusBadge";

// El backend devuelve mensajes técnicos en inglés (consistente con el resto
// del portfolio: código/logs en inglés). Estos dos son los únicos que llegan
// al usuario en la UI, así que se traducen acá — el resto de los mensajes de
// Docker.DotNet/Npgsql (timeouts, errores de red) quedan sin traducir porque
// son impredecibles y no vale la pena un mapeo frágil.
const KNOWN_ERRORS: Record<string, string> = {
  "Container not found": "Contenedor no encontrado",
  "No password configured": "Sin credenciales configuradas",
};

function translateError(error: string | null, fallback: string): string {
  if (!error) return fallback;
  return KNOWN_ERRORS[error] ?? error;
}

function containerDotColor(container: ContainerStatus): string {
  if (container.found && container.state === "running") return "bg-emerald-500";
  if (container.found) return "bg-amber-500";
  return "bg-red-500";
}

function containerLabel(container: ContainerStatus): string {
  if (container.found && container.state === "running") return `running · ${formatUptime(container.startedAt)}`;
  if (container.found) return container.state ?? "desconocido";
  return translateError(container.error, "no encontrado");
}

function DetailRow({ label, dotColor, detail }: { label: string; dotColor: string; detail: string }) {
  return (
    <div className="flex items-center justify-between py-1.5 text-sm">
      <span className="flex items-center gap-2 text-zinc-500 dark:text-zinc-400">
        <span className={`h-2 w-2 rounded-full ${dotColor}`} />
        {label}
      </span>
      <span className="text-zinc-700 dark:text-zinc-300">{detail}</span>
    </div>
  );
}

function databaseDetail(database: DatabaseStatus): string {
  if (!database.reachable) return translateError(database.error, "sin conexión");
  const parts = [formatBytes(database.sizeBytes)];
  if (database.activeConnections !== null) parts.push(`${database.activeConnections} conexiones`);
  return parts.join(" · ");
}

export function ProjectCard({ project }: { project: ProjectStatus }) {
  return (
    <div className="rounded-xl border border-zinc-200 bg-white p-5 shadow-sm dark:border-zinc-800 dark:bg-zinc-900">
      <div className="flex items-center justify-between">
        <h2 className="font-semibold text-zinc-900 dark:text-zinc-50">{project.name}</h2>
        <StatusBadge state={project.health.state} />
      </div>

      {project.health.responseTimeMs !== null && (
        <p className="mt-0.5 text-xs text-zinc-400">{project.health.responseTimeMs} ms</p>
      )}

      <div className="mt-3 divide-y divide-zinc-100 dark:divide-zinc-800">
        <DetailRow label="API" dotColor={containerDotColor(project.apiContainer)} detail={containerLabel(project.apiContainer)} />
        <DetailRow label="Base de datos (contenedor)" dotColor={containerDotColor(project.dbContainer)} detail={containerLabel(project.dbContainer)} />
        <DetailRow
          label="Base de datos (datos)"
          dotColor={project.database.reachable ? "bg-emerald-500" : "bg-red-500"}
          detail={databaseDetail(project.database)}
        />
      </div>
    </div>
  );
}
