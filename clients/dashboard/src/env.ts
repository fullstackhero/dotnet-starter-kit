// Dev builds proxy `/api` → VITE_API_BASE_URL via vite.config.ts, so the app can use relative URLs.
// Production builds should set VITE_API_BASE_URL to the fully-qualified API origin.
const apiBase = (import.meta.env.VITE_API_BASE_URL ?? "").replace(/\/$/, "");

export const env = {
  apiBase,
  defaultTenant: import.meta.env.VITE_DEFAULT_TENANT ?? "root",
};
