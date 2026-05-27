import { tokenStore } from "@/auth/token-store";

/**
 * Cross-app impersonation handoff. The admin app issues an impersonation
 * access token server-side, then opens the dashboard with the token in the
 * URL hash:
 *
 *   https://dashboard.example.com/#impersonate?token=<jwt>&tenant=<id>&expiresAt=<iso>
 *
 * We use the hash (not query) for two reasons:
 *   1. Browsers never send the fragment in HTTP requests, so the token can't
 *      leak via referrer headers or server access logs.
 *   2. SPA hash routes are already a thing — the bootstrap can scrub the
 *      hash before any router runs, without touching the path.
 *
 * Call this synchronously in main.tsx BEFORE createRoot so the token is
 * installed before AuthProvider's first render — otherwise ProtectedRoute
 * would see an anonymous session, redirect to /login, and the user would
 * have to sign in even though we have a valid impersonation token.
 */
export function installImpersonationFromHash(): void {
  if (typeof window === "undefined") return;
  const hash = window.location.hash;
  if (!hash.startsWith("#impersonate?")) return;

  const params = new URLSearchParams(hash.slice("#impersonate?".length));
  const token = params.get("token");
  const tenant = params.get("tenant");
  if (!token) {
    // Malformed handoff — strip the hash and let the normal sign-in flow
    // take over rather than getting stuck.
    stripHash();
    return;
  }

  // beginImpersonation stashes the currently-installed actor tokens (if any)
  // before swapping. In the typical cross-app handoff there are none — this
  // is a fresh tab — so the stash is a no-op. When the user later clicks
  // End-impersonation, the dashboard's stopImpersonation() calls the server
  // which mints a real actor token+refresh for the admin operator's account.
  tokenStore.beginImpersonation(token, tenant);
  stripHash();
}

function stripHash(): void {
  try {
    const cleaned = `${window.location.pathname}${window.location.search}`;
    window.history.replaceState(null, "", cleaned);
  } catch {
    // history API unavailable (file://, sandboxed iframe); fall back to a
    // plain location.hash assignment which adds a history entry but at
    // least removes the secret from the URL bar.
    window.location.hash = "";
  }
}
