import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { App } from "@/App";
import { installImpersonationFromHash } from "@/auth/impersonation-handoff";
import "@/styles/globals.css";

const rootElement = document.getElementById("root");
if (!rootElement) {
  throw new Error("Root element '#root' not found");
}

// Cross-app impersonation handoff — must run BEFORE createRoot so the
// installed token is visible to AuthProvider on first paint. See the
// helper docstring for the why.
installImpersonationFromHash();

createRoot(rootElement).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
