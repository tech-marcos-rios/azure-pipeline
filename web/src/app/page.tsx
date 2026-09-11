"use client";

import { useQuery } from "@tanstack/react-query";
import { RefreshCw } from "lucide-react";
import type { PortfolioStatus } from "@/lib/types";
import { formatTime } from "@/lib/format";
import { DiskUsageBar } from "@/components/DiskUsageBar";
import { ProjectCard } from "@/components/ProjectCard";

const POLL_INTERVAL_MS = 15_000;

async function fetchStatus(): Promise<PortfolioStatus> {
  const res = await fetch("/api/status", { cache: "no-store" });
  if (!res.ok) throw new Error(`El backend respondió ${res.status}`);
  return res.json();
}

export default function Home() {
  const { data, error, isLoading, isFetching } = useQuery({
    queryKey: ["portfolio-status"],
    queryFn: fetchStatus,
    refetchInterval: POLL_INTERVAL_MS,
  });

  return (
    <main className="mx-auto w-full max-w-4xl flex-1 px-6 py-10">
      <div className="flex items-center justify-between">
        <h1 className="text-xl font-semibold text-zinc-900 dark:text-zinc-50">Portfolio Ops</h1>
        {data && (
          <span className="flex items-center gap-1.5 text-xs text-zinc-400">
            <RefreshCw className={`h-3.5 w-3.5 ${isFetching ? "animate-spin" : ""}`} />
            {formatTime(data.checkedAt)}
          </span>
        )}
      </div>

      {isLoading && <p className="mt-8 text-sm text-zinc-500">Cargando estado…</p>}

      {error && (
        <p className="mt-8 rounded-lg bg-red-50 px-4 py-3 text-sm text-red-700 dark:bg-red-950 dark:text-red-400">
          No se pudo cargar el estado: {error.message}
        </p>
      )}

      {data && (
        <div className="mt-6 flex flex-col gap-4">
          <DiskUsageBar disk={data.disk} />
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            {data.projects.map((project) => (
              <ProjectCard key={project.name} project={project} />
            ))}
          </div>
        </div>
      )}
    </main>
  );
}
