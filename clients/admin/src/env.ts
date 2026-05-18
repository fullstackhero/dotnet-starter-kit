// Dev builds proxy `/api` → VITE_API_BASE_URL via vite.config.ts, so the app can use relative URLs.
// Production builds should set VITE_API_BASE_URL to the fully-qualified API origin.
const apiBase = (import.meta.env.VITE_API_BASE_URL ?? "").replace(/\/$/, "");

// Tenant dashboard origin used by impersonation handoff. Dev default mirrors
// `clients/dashboard/package.json` dev port. Production deployments should set
// VITE_DASHBOARD_URL explicitly to the dashboard's public origin so the new
// tab opens on the right host.
const dashboardUrl = (import.meta.env.VITE_DASHBOARD_URL ?? "http://localhost:5174").replace(/\/$/, "");

export const env = {
  apiBase,
  defaultTenant: import.meta.env.VITE_DEFAULT_TENANT ?? "root",
  dashboardUrl,
};
