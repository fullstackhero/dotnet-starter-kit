import { useNavigate } from "react-router-dom";
import { Building2, LogIn } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useAuth } from "@/auth/use-auth";

/**
 * TenantDeactivatedPage — calm centered terminal-state card shown when the
 * signed-in user's tenant is switched off mid-session.
 *
 * The deactivated-tenant guard (MultitenancyModule) starts rejecting every
 * request with a 403 once a tenant is deactivated. A global query/mutation
 * error hook (query-client.ts) detects that 403 and routes here, replacing the
 * dead error banner that would otherwise sit under a half-loaded dashboard.
 *
 * Mounted as a top-level route (outside ProtectedRoute / AppShell): the access
 * token is still technically valid, but nothing the user can do inside the app
 * will work, so there is no shell to render. The single action clears the
 * session and returns to sign-in. Mirrors the not-found.tsx empty-state
 * vocabulary: one muted icon tile, an Outfit headline, a soft body line, and a
 * single primary affordance.
 */
export function TenantDeactivatedPage() {
  const navigate = useNavigate();
  const { logout } = useAuth();

  const backToSignIn = () => {
    // Drop the now-useless session so /login starts clean and a stale token
    // can't bounce the user straight back into a 403 loop.
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
          <Building2 className="size-6 text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.5)]" />
        </div>

        <h1 className="mb-2 font-display text-display-stat font-semibold tracking-tight text-[var(--color-foreground)]">
          Tenant deactivated
        </h1>
        <p className="mb-7 max-w-[380px] text-[14px] leading-relaxed text-[var(--color-muted-foreground)]">
          This tenant has been deactivated and is no longer available. If you
          think this is a mistake, please contact your administrator.
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
      </div>
    </div>
  );
}
