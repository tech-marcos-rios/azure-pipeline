export type HealthState = "Healthy" | "Degraded" | "Unhealthy" | "Unknown";

export interface HealthCheckResult {
  state: HealthState;
  statusCode: number | null;
  responseTimeMs: number | null;
  error: string | null;
}

export interface ContainerStatus {
  name: string;
  found: boolean;
  state: string | null;
  startedAt: string | null;
  error: string | null;
}

export interface DatabaseStatus {
  reachable: boolean;
  sizeBytes: number | null;
  activeConnections: number | null;
  error: string | null;
}

export interface DiskStatus {
  totalBytes: number;
  freeBytes: number;
  usedPercent: number;
  error: string | null;
}

export interface ProjectStatus {
  name: string;
  health: HealthCheckResult;
  apiContainer: ContainerStatus;
  dbContainer: ContainerStatus;
  database: DatabaseStatus;
}

export interface PortfolioStatus {
  checkedAt: string;
  disk: DiskStatus;
  projects: ProjectStatus[];
}
