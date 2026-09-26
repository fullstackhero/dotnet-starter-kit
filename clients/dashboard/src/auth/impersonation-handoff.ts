import { tokenStore } from "@/auth/token-store";
import i18n, { SUPPORTED } from "@/i18n";

/**
 * Cross-app impersonation handoff. The admin app issues an impersonation
 * access token server-side, then opens the dashboard with the token in the
 * URL hash:
 *
 *   https://dashboard.example.com/#impersonate?token=<jwt>&tenant=<id>&expiresAt=<iso>&locale=<tag>
 *
 * We use the hash (not query) for two reasons:
 *   1. Browsers never send the fragment in HTTP requests, so the token can't
 *      leak via referrer headers or server access logs.
 *   2. SPA hash routes are already a thing — the bootstrap can scrub the
 *      hash before any router runs, without touching the path.
 *
 * Await this in main.tsx BEFORE createRoot so both the token and the language
 * are installed before AuthProvider's first render — otherwise ProtectedRoute
 * would see an anonymous session, redirect to /login, and the user would
 * have to sign in even though we have a valid impersonation token.
 */
export async function installImpersonationFromHash(): Promise<void> {
  if (typeof window === "undefined") return;
  const hash = window.location.hash;
  if (!hash.startsWith("#impersonate?")) return;

  const params = new URLSearchParams(hash.slice("#impersonate?".length));
  const token = params.get("token");
  const tenant = params.get("tenant");
  const locale = params.get("locale");
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

  // Adopt the operator's language for the impersonation session. The server
  // strips the target's `locale` claim on purpose, and this app is normally on a
  // different origin than admin, so the handoff parameter is the only channel
  // that carries the operator's choice. Applying it here also fixes the API
  // side: apiFetch derives Accept-Language from i18n.language, so responses come
  // back in the operator's language instead of the browser-detected one.
  // Unsupported or absent tags are ignored, leaving normal detection in place.
  if (locale && (SUPPORTED as readonly string[]).includes(locale) && i18n.language !== locale) {
    await i18n.changeLanguage(locale);
  }
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
