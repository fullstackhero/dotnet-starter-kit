import {
  useEffect,
  useRef,
  useState,
  type FormEvent,
  type RefObject,
} from "react";
import { Navigate, useLocation, useNavigate } from "react-router-dom";
import {
  AlertCircle,
  ArrowRight,
  Loader2,
  ShieldCheck,
  Sparkles,
} from "lucide-react";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";
import { env } from "@/env";

type LocationState = { from?: { pathname: string } };

// Dev-only seeded credentials — match what IdentityDbInitializer creates for
// the root tenant. Surfaced as a one-click button below; never shipped in
// production bundles because Vite statically replaces import.meta.env.DEV
// with false during `vite build`, so the entire branch is dead-code-eliminated.
const DEFAULT_DEV_CREDENTIALS = {
  email: "admin@root.com",
  password: "123Pa$$word!",
} as const;

/**
 * Updates `--mx`/`--my` on the supplied element so a CSS radial spotlight
 * tracks the cursor. Listener detaches on unmount.
 */
function useSpotlight<T extends HTMLElement>(): RefObject<T | null> {
  const ref = useRef<T>(null);
  useEffect(() => {
    const el = ref.current;
    if (!el) return undefined;
    const onMove = (e: MouseEvent) => {
      const rect = el.getBoundingClientRect();
      el.style.setProperty("--mx", `${e.clientX - rect.left}px`);
      el.style.setProperty("--my", `${e.clientY - rect.top}px`);
    };
    el.addEventListener("mousemove", onMove);
    return () => el.removeEventListener("mousemove", onMove);
  }, []);
  return ref;
}

/**
 * Sets `--px`/`--py` on the document root in the range [-1, 1] based on
 * the cursor's normalized viewport position. The aurora orbs read these
 * vars to translate-3d, producing a soft parallax depth illusion.
 * rAF-coalesced so the listener stays cheap.
 */
function useViewportParallax() {
  useEffect(() => {
    let rafId = 0;
    const onMove = (e: MouseEvent) => {
      if (rafId) return;
      rafId = requestAnimationFrame(() => {
        const xn = (e.clientX / window.innerWidth - 0.5) * 2;
        const yn = (e.clientY / window.innerHeight - 0.5) * 2;
        document.documentElement.style.setProperty("--px", String(xn));
        document.documentElement.style.setProperty("--py", String(yn));
        rafId = 0;
      });
    };
    window.addEventListener("mousemove", onMove, { passive: true });
    return () => {
      window.removeEventListener("mousemove", onMove);
      if (rafId) cancelAnimationFrame(rafId);
      document.documentElement.style.removeProperty("--px");
      document.documentElement.style.removeProperty("--py");
    };
  }, []);
}

/**
 * FloatField — modern floating-label input. The label sits centered in
 * the field at rest and translates up + shrinks + brand-tints when the
 * input gains focus or has a value. Driven by :placeholder-shown so no
 * JS state coordination is needed.
 */
function FloatField({
  id,
  label,
  ...inputProps
}: {
  id: string;
  label: string;
} & React.InputHTMLAttributes<HTMLInputElement>) {
  return (
    <div className="float-field">
      <input id={id} placeholder=" " {...inputProps} />
      <label htmlFor={id}>{label}</label>
    </div>
  );
}

export function LoginPage() {
  const { isAuthenticated, login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as LocationState | null)?.from?.pathname ?? "/";

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [tenant, setTenant] = useState(env.defaultTenant);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useViewportParallax();
  const cardRef = useSpotlight<HTMLDivElement>();

  if (isAuthenticated) {
    return <Navigate to={from} replace />;
  }

  const performLogin = async (creds: { email: string; password: string; tenant: string }) => {
    setError(null);
    setSubmitting(true);
    try {
      await login(creds);
      navigate(from, { replace: true });
    } catch (err) {
      const message =
        err instanceof ApiRequestError
          ? err.problem?.detail ?? err.problem?.title ?? err.message
          : err instanceof Error
            ? err.message
            : "Login failed";
      setError(message);
    } finally {
      setSubmitting(false);
    }
  };

  const onSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    await performLogin({ email, password, tenant });
  };

  const onSignInAsDefault = async () => {
    await performLogin({
      email: DEFAULT_DEV_CREDENTIALS.email,
      password: DEFAULT_DEV_CREDENTIALS.password,
      tenant: env.defaultTenant,
    });
  };

  return (
    <div className="relative grid min-h-screen place-items-center overflow-hidden px-6 py-12">
      {/* Atmospheric background — two parallax aurora orbs that drift
          slightly opposite to cursor for soft depth. The body's noise
          layer remains underneath via globals.css. */}
      <div
        aria-hidden
        className="parallax-orb pointer-events-none absolute -left-32 -top-32 h-[520px] w-[520px] rounded-full blur-[140px]"
        style={{ backgroundColor: "oklch(from var(--color-primary) l c h / 0.32)" }}
      />
      <div
        aria-hidden
        className="parallax-orb-2 pointer-events-none absolute -right-32 -bottom-40 h-[560px] w-[560px] rounded-full blur-[160px]"
        style={{ backgroundColor: "oklch(0.700 0.155 195 / 0.22)" }}
      />

      {/* The card — glow-frame outer ring + glassmorphism surface +
          cursor spotlight. */}
      <div className="glow-frame fsh-enter relative z-10 w-full max-w-[420px] shadow-[var(--shadow-lift)]">
        <div
          ref={cardRef}
          className={cn(
            "card-spotlight rounded-[calc(var(--radius-2xl)-1px)]",
            // Translucent surface + saturating backdrop blur — atmosphere
            // shows through the card.
            "bg-[oklch(from_var(--color-card)_l_c_h_/_0.72)] backdrop-blur-2xl backdrop-saturate-150",
            "px-8 pt-8 pb-7",
          )}
        >
          {/* Brand mark — small, centered, animated conic underneath. */}
          <div className="mb-7 flex flex-col items-center gap-3 fsh-enter fsh-enter-1">
            <span
              aria-hidden
              className={cn(
                "brand-mark grid h-9 w-9 place-items-center rounded-lg",
                "text-[14px] font-bold tracking-tight text-[var(--color-primary-foreground)]",
                "shadow-[0_1px_0_oklch(1_0_0_/_0.20)_inset,0_8px_22px_-8px_oklch(from_var(--color-primary)_l_c_h_/_0.65)]",
              )}
            >
              F
            </span>
            <span className="font-mono text-[10.5px] uppercase tracking-[0.16em] text-[var(--color-muted-foreground)]">
              FullStackHero · console
            </span>
          </div>

          {/* Heading — gradient on the second word. Display-weight,
              clamp() for fluid sizing on narrow viewports. */}
          <header className="mb-7 space-y-2 text-center fsh-enter fsh-enter-2">
            <h1
              className="text-display font-semibold leading-[1.05]"
              style={{ fontSize: "clamp(1.5rem, 1.2rem + 1.4vw, 1.875rem)" }}
            >
              Welcome <span className="text-gradient-brand">back.</span>
            </h1>
            <p className="text-sm leading-relaxed text-[var(--color-muted-foreground)]">
              Sign in to your tenant to continue.
            </p>
          </header>

          <form onSubmit={onSubmit} className="space-y-3" noValidate>
            <div className="fsh-enter fsh-enter-3">
              <FloatField
                id="tenant"
                label="Tenant"
                value={tenant}
                onChange={(e) => setTenant(e.target.value)}
                required
                autoComplete="organization"
              />
            </div>

            <div className="fsh-enter fsh-enter-3">
              <FloatField
                id="email"
                label="Email"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                autoComplete="email"
              />
            </div>

            <div className="fsh-enter fsh-enter-4">
              <FloatField
                id="password"
                label="Password"
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                autoComplete="current-password"
              />
            </div>

            {error && (
              <div
                role="alert"
                className={cn(
                  "fsh-enter flex items-start gap-2 rounded-md border px-3 py-2 text-sm",
                  "border-[oklch(from_var(--color-destructive)_l_c_h_/_0.40)]",
                  "bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.08)]",
                  "text-[var(--color-destructive)]",
                )}
              >
                <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" />
                <span className="leading-snug">{error}</span>
              </div>
            )}

            <div className="space-y-2.5 pt-3 fsh-enter fsh-enter-5">
              <Button
                type="submit"
                size="lg"
                className="btn-shimmer w-full"
                disabled={submitting || !email || !password || !tenant}
              >
                {submitting ? (
                  <>
                    <Loader2 className="h-4 w-4 animate-spin" />
                    Signing in…
                  </>
                ) : (
                  <>
                    Sign in
                    <ArrowRight className="h-4 w-4 transition-transform duration-[var(--duration-default)] group-hover/btn:translate-x-0.5" />
                  </>
                )}
              </Button>

              {import.meta.env.DEV && (
                <Button
                  type="button"
                  variant="soft"
                  className="w-full"
                  disabled={submitting}
                  onClick={onSignInAsDefault}
                >
                  <Sparkles className="h-3.5 w-3.5" />
                  Continue as default admin
                </Button>
              )}
            </div>
          </form>

          {/* Trust strip */}
          <div className="mt-7 flex items-center justify-center gap-1.5 text-[11px] tracking-tight text-[var(--color-muted-foreground)] fsh-enter fsh-enter-5">
            <ShieldCheck className="h-3 w-3" />
            <span>Encrypted in transit · JWT-secured session</span>
          </div>
        </div>
      </div>

      {/* Footer */}
      <div className="absolute bottom-6 left-1/2 z-10 -translate-x-1/2 text-center text-xs text-[var(--color-muted-foreground)]">
        <span>
          Need a tenant?{" "}
          <a
            href="https://fullstackhero.net"
            target="_blank"
            rel="noreferrer"
            className="text-[var(--color-foreground)] underline-offset-4 hover:underline"
          >
            Get started
          </a>
        </span>
        <span className="mx-3 text-[var(--color-border-strong)]">·</span>
        <span className="font-mono">v0.1</span>
      </div>
    </div>
  );
}
