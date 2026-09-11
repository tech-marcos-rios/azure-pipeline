import type { DiskStatus } from "@/lib/types";
import { formatBytes } from "@/lib/format";

export function DiskUsageBar({ disk }: { disk: DiskStatus }) {
  if (disk.error) {
    return <p className="text-sm text-red-600 dark:text-red-400">Disco: {disk.error}</p>;
  }

  const barColor = disk.usedPercent >= 90 ? "bg-red-500" : disk.usedPercent >= 75 ? "bg-amber-500" : "bg-emerald-500";

  return (
    <div className="rounded-xl border border-zinc-200 bg-white p-5 shadow-sm dark:border-zinc-800 dark:bg-zinc-900">
      <div className="flex items-baseline justify-between">
        <h2 className="font-semibold text-zinc-900 dark:text-zinc-50">Disco del server</h2>
        <span className="text-sm text-zinc-500 dark:text-zinc-400">
          {formatBytes(disk.totalBytes - disk.freeBytes)} / {formatBytes(disk.totalBytes)} ({disk.usedPercent}%)
        </span>
      </div>
      <div className="mt-3 h-2 w-full overflow-hidden rounded-full bg-zinc-100 dark:bg-zinc-800">
        <div className={`h-full rounded-full ${barColor}`} style={{ width: `${Math.min(disk.usedPercent, 100)}%` }} />
      </div>
    </div>
  );
}
