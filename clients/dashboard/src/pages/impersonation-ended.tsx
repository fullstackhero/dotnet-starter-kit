import { useLocation, useNavigate } from "react-router-dom";
import { LogIn, ShieldOff } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useAuth } from "@/auth/use-auth";

/**
 * ImpersonationEndedPage — calm centered terminal-state card shown when an
 * operator's impersonation grant is revoked (or the short-lived impersonation
 * token expires) mid-session.
 *
 * The server's OnTokenValidated hook (ConfigureJwtBearerOptions) starts
 * rejecting the impersonation token with a 401 the moment its grant is
 * revoked/ended. Impersonation sessions carry no refresh token, so that 401
 * propagates straight through the api client; a global query/mutation error
 * hook (query-client.ts) detects it (isImpersonationRevokedError) and routes
 * here — replacing the dead error banner that would otherwise sit under a
 * half-loaded dashboard while the impersonation banner lingered.
 *
 * Mounted as a top-level route (outside ProtectedRoute / AppShell): the access
 * token is still technically decodable, but nothing the impersonated identity
 * can do will work, so there is no shell to render. The single action clears
 * the dead session and returns to sign-in. Mirrors tenant-deactivated.tsx's
 * empty-state vocabulary: one muted icon tile, an Outfit headline, a soft body
 * line, and a single primary affordance.
 */
export function ImpersonationEndedPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const { logout } = useAuth();

  // The global error hook passes the dev-only JwtBearer rejection reason via
  // router state. Production blanks it, so the body copy stands on its own and
  // this only renders as a subtle diagnostic line when present.
  const reason =
    typeof (location.state as { reason?: unknown } | null)?.reason === "string"
      ? (location.state as { reason: string }).reason
      : undefined;

  const backToSignIn = () => {
    // Drop the now-useless impersonation token so /login starts clean and a
    // stale token can't bounce the user straight back into a 401 loop.
    logout();
    navigate("/login", { replace: true });
  };

  return (
    <div className="relative flex min-h-screen items-center justify-center overflow-hidden bg-[var(--color-background)] px-5 py-8 sm:py-12">
      {/* Atmospheric background — same rose/saffron orbs as the auth pages */}
      <div className="pointer-events-none absolute inset-0" aria-hidden>
        <div
          className="absolute -top-[25%] -left-[15%] h-[70vw] w-[70vw] rounded-full blur-[140px]"
          style={{ backgroundColor: "oklch(from var(--color-primary) l c h / 0.05)" }}
        />
        <div
          className="absolute -bottom-[20%] -right-[10%] h-[55vw] w-[55vw] rounded-full blur-[120px]"
          style={{ backgroundColor: "oklch(from var(--color-saffron) l c h / 0.07)" }}
        />
      </div>

      <div className="relative z-10 flex w-full max-w-[460px] flex-col items-center text-center fsh-enter fsh-enter-1">
        {/* Icon tile — same muted bg + size-14 rounded-2xl as the not-found page */}
        <div className="mb-5 grid size-14 place-items-center rounded-2xl bg-[var(--color-muted)]">
          <ShieldOff className="size-6 text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.5)]" />
        </div>

        <h1 className="mb-2 font-display text-display-stat font-semibold tracking-tight text-[var(--color-foreground)]">
          Impersonation ended
        </h1>
        <p className="mb-7 max-w-[380px] text-[14px] leading-relaxed text-[var(--color-muted-foreground)]">
          The operator's access to this session was revoked or has expired. Sign
          in with your own account to continue.
        </p>

        {/* Primary action */}
        <Button
          type="button"
          onClick={backToSignIn}
          className="group h-11 px-5 text-[14px] font-semibold"
        >
          <LogIn className="size-4" />
          <span>Back to sign in</span>
        </Button>

        {/* Dev-only diagnostic — the JwtBearer rejection reason. Hidden in prod
            (the server blanks it) and even when present is whispered, not shouted. */}
        {import.meta.env.DEV && reason && (
          <p className="mt-6 max-w-[380px] font-mono text-[11px] leading-relaxed text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.6)]">
            {reason}
          </p>
        )}
      </div>
    </div>
  );
}
