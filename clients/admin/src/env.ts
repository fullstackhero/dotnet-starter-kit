// Runtime config — fetched once at boot from /config.json (served by the
// frontend's own nginx in production, by Vite from public/config.json in
// dev). One image works for every deploy; the operator wires API_URL /
// DASHBOARD_URL into the JSON file via envsubst at container start.
type RuntimeConfig = {
  apiBase: string;
  defaultTenant: string;
  dashboardUrl: string;
  /** Idle time (ms) before the inactivity warning appears. Admin = sensitive operator console. */
  inactivityIdleMs: number;
  /** Warning-countdown length (ms) before auto sign-out. */
  inactivityWarningMs: number;
};

// Admin defaults: 10 minutes idle, then a 60-second warning.
const DEFAULT_INACTIVITY_IDLE_MS = 10 * 60_000;
const DEFAULT_INACTIVITY_WARNING_MS = 60_000;

/** Accept a positive finite number from config, else fall back. */
function positiveOr(value: unknown, fallback: number): number {
  return typeof value === "number" && Number.isFinite(value) && value > 0 ? value : fallback;
}

let cached: RuntimeConfig | null = null;

export async function loadRuntimeConfig(): Promise<void> {
  if (cached !== null) return;
  const res = await fetch("/config.json", { cache: "no-store" });
  if (!res.ok) {
    throw new Error(`Failed to load /config.json: ${res.status} ${res.statusText}`);
  }
  const cfg = (await res.json()) as Partial<RuntimeConfig>;
  cached = {
    apiBase: (cfg.apiBase ?? "").replace(/\/$/, ""),
    defaultTenant: cfg.defaultTenant ?? "root",
    // Dashboard origin used by the impersonation handoff. Dev default
    // mirrors clients/dashboard/package.json's vite port.
    dashboardUrl: (cfg.dashboardUrl ?? "http://localhost:5174").replace(/\/$/, ""),
    inactivityIdleMs: positiveOr(cfg.inactivityIdleMs, DEFAULT_INACTIVITY_IDLE_MS),
    inactivityWarningMs: positiveOr(cfg.inactivityWarningMs, DEFAULT_INACTIVITY_WARNING_MS),
  };
}

function get(): RuntimeConfig {
  if (cached === null) {
    throw new Error(
      "Runtime config not loaded. main.tsx must await loadRuntimeConfig() before mounting React.",
    );
  }
  return cached;
}

export const env = {
  get apiBase(): string { return get().apiBase; },
  get defaultTenant(): string { return get().defaultTenant; },
  get dashboardUrl(): string { return get().dashboardUrl; },
  get inactivityIdleMs(): number { return get().inactivityIdleMs; },
  get inactivityWarningMs(): number { return get().inactivityWarningMs; },
};
