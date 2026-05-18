// Runtime config — fetched once at boot from /config.json (served by the
// frontend's own nginx in production, by Vite from public/config.json in
// dev). One image works for every deploy; the operator wires API_URL /
// DASHBOARD_URL into the JSON file via envsubst at container start.
type RuntimeConfig = {
  apiBase: string;
  defaultTenant: string;
  dashboardUrl: string;
};

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
};
