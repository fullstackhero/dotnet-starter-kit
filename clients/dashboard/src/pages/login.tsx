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
} from "lucide-react";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";
import { env } from "@/env";
import { LoginBrandStory } from "@/pages/login.brand-story";
import { LoginDemoPanel } from "@/pages/login.demo-panel";
import type { DemoAccount } from "@/pages/login.demo-accounts";

type LocationState = { from?: { pathname: string } };

// ────────────────────────────────────────────────────────────────────────
// Tech-stack ribbon at the bottom of the canvas. Slow horizontal
// auto-scroll powered by globals.css `.fsh-marquee`. Caller renders
// the list twice for a seamless loop. Hovering pauses the scroll.
// ────────────────────────────────────────────────────────────────────────

const STACK: ReadonlyArray<string> = [
  ".NET 10",
  "TypeScript",
  "PostgreSQL",
  "Aspire",
  "Mediator",
  "EF Core 10",
  "Finbuckle",
  "Hangfire",
  "Redis",
  "OpenAPI 3.1",
  "Scalar",
  "Serilog",
  "OpenTelemetry",
  "JWT · OIDC",
  "FluentValidation",
  "xUnit · Testcontainers",
  "Docker",
  "React 19 · Vite 7",
  "Tailwind 4",
  "TanStack Query",
];

function TechMarquee() {
  return (
    <div
      aria-hidden
      className={cn(
        "pointer-events-auto relative w-full overflow-hidden",
        "border-y border-[var(--color-border)]/60",
        "bg-[oklch(from_var(--color-background)_l_c_h_/_0.55)] backdrop-blur-md",
      )}
      style={{
        // Fade-out on both edges so the scroll feels infinite — no hard
        // start/end. The mask narrows the visible band to the middle.
        WebkitMaskImage:
          "linear-gradient(90deg, transparent, black 8%, black 92%, transparent)",
        maskImage:
          "linear-gradient(90deg, transparent, black 8%, black 92%, transparent)",
      }}
    >
      <div className="fsh-marquee gap-7 py-3">
        {[...STACK, ...STACK].map((label, i) => (
          <span
            key={`${label}-${i}`}
            className={cn(
              "shrink-0 font-mono text-[11.5px] uppercase tracking-[0.16em] text-[var(--color-muted-foreground)]",
              "inline-flex items-center gap-2 whitespace-nowrap",
            )}
          >
            <span
              aria-hidden
              className="inline-block h-1 w-1 rounded-full bg-[var(--color-border-strong)]"
            />
            {label}
          </span>
        ))}
      </div>
    </div>
  );
}

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
 * FloatField — modern floating-label input. Label sits centered in the
 * field at rest and translates up + shrinks + brand-tints when the
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

// ────────────────────────────────────────────────────────────────────────
// Top strip — single brand mark on the left, build/version chip on the
// right. Replaces the duplicate brand mark that previously appeared
// inside the form card.
// ────────────────────────────────────────────────────────────────────────

function TopStrip() {
  return (
    <div className="relative z-10 flex items-center justify-between px-6 pt-6 sm:px-10 sm:pt-8">
      <div className="flex items-center gap-2.5">
        <span
          aria-hidden
          className={cn(
            "brand-mark grid h-8 w-8 place-items-center rounded-md",
            "text-[12px] font-bold tracking-tight text-[var(--color-primary-foreground)]",
            "shadow-[0_1px_0_oklch(1_0_0_/_0.20)_inset,0_8px_22px_-8px_oklch(from_var(--color-primary)_l_c_h_/_0.60)]",
          )}
        >
          F
        </span>
        <span className="hidden font-semibold tracking-tight sm:inline">
          fullstackhero
        </span>
      </div>

      <div className="flex items-center gap-2 font-mono text-[10.5px] uppercase tracking-[0.16em] text-[var(--color-muted-foreground)]">
        <span
          aria-hidden
          className="pulse-dot inline-block h-1.5 w-1.5 rounded-full"
          style={{ backgroundColor: "var(--color-success)", color: "var(--color-success)" }}
        />
        <span>Service ready</span>
        <span aria-hidden className="mx-1 h-3 w-px bg-[var(--color-border-strong)]" />
        <code className="rounded bg-[var(--color-muted)] px-1.5 py-0.5">v0.1</code>
      </div>
    </div>
  );
}

// ────────────────────────────────────────────────────────────────────────
// Page
// ────────────────────────────────────────────────────────────────────────

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

  const onPickDemo = (account: DemoAccount) => {
    setError(null);
    setEmail(account.email);
    setPassword(account.password);
    setTenant(account.tenant);
  };

  // Layout breakpoints:
  //   narrow:        single column, brand-story hidden, form leads
  //   ≥lg non-DEV:   2-col [story | form], max-w-1200, centred
  //   ≥xl DEV:       3-col [story | form | demo], max-w-1440
  // The DEV demo panel is hidden below xl so it never disrupts the
  // composition on smaller screens; users running the dev server on
  // a laptop see it kick in once they hit ~1280px wide.
  return (
    <div className="relative flex min-h-screen flex-col overflow-hidden">
      {/* Atmospheric background — multi-orb aurora that drifts with the
          cursor. Layered radial gradients hand-tuned to stay soft on
          both light + dark canvases. */}
      <div
        aria-hidden
        className="parallax-orb pointer-events-none absolute -left-32 -top-40 h-[640px] w-[640px] rounded-full blur-[160px]"
        style={{ backgroundColor: "oklch(from var(--color-primary) l c h / 0.30)" }}
      />
      <div
        aria-hidden
        className="parallax-orb-2 pointer-events-none absolute -right-40 top-1/3 h-[680px] w-[680px] rounded-full blur-[180px]"
        style={{ backgroundColor: "oklch(0.700 0.155 195 / 0.22)" }}
      />
      <div
        aria-hidden
        className="parallax-orb pointer-events-none absolute left-1/3 -bottom-40 h-[560px] w-[560px] rounded-full blur-[160px]"
        style={{ backgroundColor: "oklch(from var(--color-primary) l c h / 0.16)" }}
      />

      <TopStrip />

      {/* Main composition — hero column + form + (DEV) demo. Sits
          centred in a max-width container, vertically pinned to the
          middle of the remaining viewport. */}
      <main className="relative z-10 flex flex-1 items-center px-6 py-12 sm:px-10">
        <div
          className={cn(
            "mx-auto grid w-full items-center gap-10",
            "max-w-[1200px]",
            "lg:grid-cols-[minmax(0,1.15fr)_minmax(0,420px)]",
            "xl:max-w-[1440px] xl:gap-12 xl:grid-cols-[minmax(0,1fr)_420px_320px]",
          )}
        >
          {/* Hero column — hidden below lg so narrow viewports lead
              with the form (faster sign-in on phones). */}
          <div className="hidden lg:block">
            <LoginBrandStory />
          </div>

          {/* Form card */}
          <div className="glow-frame fsh-enter fsh-enter-2 relative w-full shadow-[var(--shadow-lift)]">
            <div
              ref={cardRef}
              className={cn(
                "card-spotlight rounded-[calc(var(--radius-2xl)-1px)]",
                // Translucent surface + saturating backdrop blur so the
                // aurora behind shows through the card. Tone-rail on the
                // left edge (3px brand-coloured border) anchors the card
                // to the brand without needing the duplicated brand
                // mark we used to render here.
                "bg-[oklch(from_var(--color-card)_l_c_h_/_0.78)] backdrop-blur-2xl backdrop-saturate-150",
                "border-l-[3px] border-l-[var(--color-primary)]",
                "px-7 pb-7 pt-7",
              )}
            >
              {/* Eyebrow — replaces the duplicate brand mark that used
                  to live here. Tone-soft mono caps. */}
              <div className="flex items-center gap-2 fsh-enter fsh-enter-3">
                <span
                  aria-hidden
                  className="inline-block h-px w-6 bg-[var(--color-primary)]"
                />
                <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.20em] text-[var(--color-primary)]">
                  Sign in
                </span>
              </div>

              <header className="mt-3 mb-7 space-y-1.5 fsh-enter fsh-enter-3">
                <h1
                  className="text-display pb-1 font-semibold leading-[1.05] tracking-[-0.022em]"
                  style={{ fontSize: "clamp(1.625rem, 1.3rem + 1.2vw, 2rem)" }}
                >
                  Welcome <span className="text-gradient-brand">back.</span>
                </h1>
                <p className="text-[13.5px] leading-relaxed text-[var(--color-muted-foreground)]">
                  Sign in to your tenant to continue.
                </p>
              </header>

              <form onSubmit={onSubmit} className="space-y-3" noValidate>
                <div className="fsh-enter fsh-enter-4">
                  <FloatField
                    id="tenant"
                    label="Tenant"
                    value={tenant}
                    onChange={(e) => setTenant(e.target.value)}
                    required
                    autoComplete="organization"
                  />
                </div>

                <div className="fsh-enter fsh-enter-4">
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

                <div className="fsh-enter fsh-enter-5">
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
                </div>
              </form>

              {/* Trust strip — moved below the CTA, lighter weight so it
                  doesn't compete with the action. */}
              <div className="mt-6 flex items-center justify-center gap-1.5 fsh-enter fsh-enter-5 text-[11px] tracking-tight text-[var(--color-muted-foreground)]">
                <ShieldCheck className="h-3 w-3" />
                <span>Encrypted in transit · JWT-secured session</span>
              </div>
            </div>
          </div>

          {/* DEV demo panel — only renders ≥xl so it never crowds the
              composition on a laptop. */}
          {import.meta.env.DEV && (
            <div className="hidden xl:block">
              <LoginDemoPanel
                current={{ email, tenant }}
                onSelect={onPickDemo}
              />
            </div>
          )}
        </div>
      </main>

      {/* Tech-stack marquee — full-bleed at the bottom of the canvas.
          Sits above the footer; pauses on hover. */}
      <TechMarquee />

      <footer className="relative z-10 px-6 py-5 text-center text-xs text-[var(--color-muted-foreground)]">
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
        <span className="font-mono">v0.1 · console</span>
      </footer>
    </div>
  );
}
