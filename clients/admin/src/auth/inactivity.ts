// Inactivity auto-logout — shared, app-agnostic primitives.
//
// localStorage/sessionStorage is per-origin, so the admin and dashboard apps
// never share these keys (different hosts). That lets us use short, un-prefixed
// key names without colliding — and, importantly, the activity heartbeat key
// does NOT start with "fsh.{app}." so it won't trip each app's auth storage
// listeners on every cross-tab tick.

/** Shared "last user activity" timestamp — one value across every tab of this origin. */
const LAST_ACTIVITY_KEY = "fsh.lastActivity";
/** Ephemeral, per-tab reason stash read once by the login page after a sign-out. */
const SIGNED_OUT_REASON_KEY = "fsh.signedOutReason";

export type InactivityPhase = "active" | "warning" | "expired";

/**
 * Pure phase evaluation — given the current time and the last activity stamp,
 * decide whether the session is active, in its warning window, or expired.
 * `secondsLeft` is the whole seconds remaining until expiry during `warning`.
 */
export function evaluateInactivity(
  now: number,
  lastActivity: number,
  idleMs: number,
  warningMs: number,
): { phase: InactivityPhase; secondsLeft: number } {
  const idle = now - lastActivity;
  if (idle >= idleMs + warningMs) return { phase: "expired", secondsLeft: 0 };
  if (idle >= idleMs) {
    const remainingMs = idleMs + warningMs - idle;
    return { phase: "warning", secondsLeft: Math.max(0, Math.ceil(remainingMs / 1000)) };
  }
  return { phase: "active", secondsLeft: 0 };
}

/** Cross-tab shared activity timestamp. Writes are best-effort (private mode). */
export const activityStore = {
  key: LAST_ACTIVITY_KEY,
  get(): number {
    try {
      const raw = localStorage.getItem(LAST_ACTIVITY_KEY);
      const parsed = raw ? Number(raw) : Number.NaN;
      return Number.isFinite(parsed) ? parsed : 0;
    } catch {
      return 0;
    }
  },
  set(ts: number): void {
    try {
      localStorage.setItem(LAST_ACTIVITY_KEY, String(ts));
    } catch {
      /* storage unavailable (private mode / quota) — degrade to single-tab timing */
    }
  },
};

/** Stash why the session ended so the login page can explain it. */
export function markSignedOut(reason: string): void {
  try {
    sessionStorage.setItem(SIGNED_OUT_REASON_KEY, reason);
  } catch {
    /* ignore */
  }
}

/** Read-and-clear the sign-out reason (one-shot). */
export function consumeSignedOutReason(): string | null {
  try {
    const value = sessionStorage.getItem(SIGNED_OUT_REASON_KEY);
    if (value) sessionStorage.removeItem(SIGNED_OUT_REASON_KEY);
    return value;
  } catch {
    return null;
  }
}
