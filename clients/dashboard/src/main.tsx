import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { App } from "@/App";
import { installImpersonationFromHash } from "@/auth/impersonation-handoff";
import { env, loadRuntimeConfig } from "@/env";
import { initI18n } from "@/i18n";
import "@/styles/globals.css";

// Runtime config must resolve before React mounts so env.apiBase reads
// inside components see the right value on first paint.
await loadRuntimeConfig();

// i18n boots AFTER config so fallbackLng can read the per-deployment default;
// the persisted/detected locale still wins over it.
await initI18n(env.defaultLanguage);

const rootElement = document.getElementById("root");
if (!rootElement) {
  throw new Error("Root element '#root' not found");
}

// Cross-app impersonation handoff — must run BEFORE createRoot so the
// installed token and the operator's language are visible to AuthProvider on
// first paint. See the helper docstring for the why.
await installImpersonationFromHash();

createRoot(rootElement).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
