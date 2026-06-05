import { useCallback } from "react";
import { useAuth } from "@/auth/use-auth";
import { env } from "@/env";
import { markSignedOut } from "@/auth/inactivity";
import { useInactivityTimeout } from "@/auth/use-inactivity-timeout";
import { InactivityDialog } from "@/components/auth/inactivity-dialog";

/**
 * InactivityGuard — mounted once inside the authenticated shell. Watches for
 * inactivity while signed in, shows the warning modal in the final window, and
 * signs the user out (with an "inactivity" reason for the login page) when the
 * countdown elapses. Durations come from runtime config so operators can tune
 * them per deployment without a rebuild.
 */
export function InactivityGuard() {
  const { isAuthenticated, logout } = useAuth();
  const idleMs = env.inactivityIdleMs;
  const warningMs = env.inactivityWarningMs;

  const signOut = useCallback(() => {
    markSignedOut("inactivity");
    logout();
  }, [logout]);

  const { phase, secondsLeft, dismiss } = useInactivityTimeout({
    enabled: isAuthenticated,
    idleMs,
    warningMs,
    onExpire: signOut,
  });

  return (
    <InactivityDialog
      open={isAuthenticated && phase === "warning"}
      secondsLeft={secondsLeft}
      totalSeconds={Math.round(warningMs / 1000)}
      onStay={dismiss}
      onSignOut={signOut}
    />
  );
}
