import { CheckCircle2, AlertTriangle, XCircle, HelpCircle } from "lucide-react";
import type { HealthState } from "@/lib/types";

const STYLES: Record<HealthState, { icon: typeof CheckCircle2; className: string; label: string }> = {
  Healthy: { icon: CheckCircle2, className: "bg-emerald-50 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-400", label: "Healthy" },
  Degraded: { icon: AlertTriangle, className: "bg-amber-50 text-amber-700 dark:bg-amber-950 dark:text-amber-400", label: "Degraded" },
  Unhealthy: { icon: XCircle, className: "bg-red-50 text-red-700 dark:bg-red-950 dark:text-red-400", label: "Unhealthy" },
  Unknown: { icon: HelpCircle, className: "bg-zinc-100 text-zinc-500 dark:bg-zinc-800 dark:text-zinc-400", label: "Unknown" },
};

export function StatusBadge({ state }: { state: HealthState }) {
  const { icon: Icon, className, label } = STYLES[state];
  return (
    <span className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-medium ${className}`}>
      <Icon className="h-3.5 w-3.5" />
      {label}
    </span>
  );
}
