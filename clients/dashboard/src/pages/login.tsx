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
  FlaskConical,
  Loader2,
  ShieldCheck,
} from "lucide-react";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
} from "@/components/ui/dialog";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";
import { env } from "@/env";
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
            className="inline-flex shrink-0 items-center gap-2 whitespace-nowrap font-mono text-[11.5px] uppercase tracking-[0.16em] text-[var(--color-muted-foreground)]"
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

// ────────────────────────────────────────────────────────────────────────
// Cursor + viewport hooks (same as before — atmosphere depends on them).
// ────────────────────────────────────────────────────────────────────────

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

// ────────────────────────────────────────────────────────────────────────
// FloatField — modern floating-label input. Driven by :placeholder-shown
// so no JS state coordination is needed.
// ────────────────────────────────────────────────────────────────────────

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
// Corner brackets — four small L-shaped marks pinned to the card's
// corners. Tone-coloured, hairline. Read as engineering / blueprint
// markers framing the card without competing with its content.
// ────────────────────────────────────────────────────────────────────────

function CornerBrackets() {
  const base = "absolute h-3 w-3 border-[var(--color-primary)] opacity-80";
  return (
    <>
      <span aria-hidden className={cn(base, "-top-px -left-px border-l border-t")} />
      <span aria-hidden className={cn(base, "-top-px -right-px border-r border-t")} />
      <span aria-hidden className={cn(base, "-bottom-px -left-px border-l border-b")} />
      <span aria-hidden className={cn(base, "-bottom-px -right-px border-r border-b")} />
    </>
  );
}

// ────────────────────────────────────────────────────────────────────────
// Top strip — brand mark left, status + version right. Single brand
// mark across the whole page.
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
// Demo popup — DEV only. Click "Demo accounts" under the form, the
// existing LoginDemoPanel renders inside a Dialog. Picking an account
// closes the dialog and prefills the form.
// ────────────────────────────────────────────────────────────────────────

function DemoDialog({
  open,
  onOpenChange,
  current,
  onSelect,
}: {
  open: boolean;
  onOpenChange: (next: boolean) => void;
  current: { email: string; tenant: string };
  onSelect: (account: DemoAccount) => void;
}) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-[520px] !p-0">
        <DialogTitle className="sr-only">Demo accounts</DialogTitle>
        <DialogDescription className="sr-only">
          Pick a seeded demo account to prefill the login form. DEV only.
        </DialogDescription>
        {/* The panel brings its own card chrome, so the dialog content
            wrapper drops its padding (!p-0) and lets the panel render
            edge-to-edge. */}
        <div className="rounded-[inherit] overflow-hidden">
          <LoginDemoPanel
            current={current}
            onSelect={(a) => {
              onSelect(a);
              onOpenChange(false);
            }}
          />
        </div>
      </DialogContent>
    </Dialog>
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
  const [demoOpen, setDemoOpen] = useState(false);

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

  return (
    <div className="relative flex min-h-screen flex-col overflow-hidden">
      {/* Atmospheric background — multi-orb aurora that drifts with the
          cursor + a subtle dot-grid overlay for the blueprint feel. */}
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

      {/* Dot grid — one tiny dot every 32px, very low opacity. Reads as
          graph paper / blueprint texture without competing for attention. */}
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 opacity-[0.65]"
        style={{
          backgroundImage:
            "radial-gradient(circle, oklch(from var(--color-foreground) l c h / 0.10) 1px, transparent 1px)",
          backgroundSize: "32px 32px",
          maskImage:
            "radial-gradient(ellipse 70% 60% at 50% 50%, black 30%, transparent 75%)",
          WebkitMaskImage:
            "radial-gradient(ellipse 70% 60% at 50% 50%, black 30%, transparent 75%)",
        }}
      />

      <TopStrip />

      {/* Main composition — single centred column. Eyebrow + headline
          set the editorial weight; the form card sits beneath them as a
          focused action surface; the editorial stat strip lives on the
          canvas below the card so it reads as page-level commentary
          rather than card content. */}
      <main className="relative z-10 flex flex-1 flex-col items-center justify-center px-6 py-10 sm:px-10">
        <div className="mx-auto flex w-full max-w-[680px] flex-col items-center text-center">
          {/* Eyebrow */}
          <div className="fsh-enter fsh-enter-1 flex items-center gap-2.5 font-mono text-[10.5px] font-medium uppercase tracking-[0.22em] text-[var(--color-muted-foreground)]">
            <span aria-hidden className="inline-block h-px w-8 bg-[var(--color-border-strong)]" />
            <span>// FullStackHero · console</span>
            <span aria-hidden className="inline-block h-px w-8 bg-[var(--color-border-strong)]" />
          </div>

          {/* Display headline — fluid clamp, gradient noun, generous
              line-height to keep descenders comfortable. */}
          <h1
            className="text-display fsh-enter fsh-enter-2 mt-6 pb-1 font-semibold leading-[1.06] tracking-[-0.025em]"
            style={{ fontSize: "clamp(2rem, 1.4rem + 2.4vw, 3.25rem)" }}
          >
            The complete{" "}
            <span className="text-gradient-brand">.NET 10</span>{" "}
            starter kit,
            <br />
            ready to ship.
          </h1>

          <p className="fsh-enter fsh-enter-3 mt-4 max-w-md text-[14px] leading-relaxed text-[var(--color-muted-foreground)]">
            Multi-tenant from day one. Modular monolith. Aspire-orchestrated.
            Sign in to continue to your tenant.
          </p>

          {/* Login card — single focal action surface, ~440px wide,
              pinned with corner brackets so it reads as a "spec sheet"
              element on the canvas rather than another generic card. */}
          <div className="fsh-enter fsh-enter-4 relative mt-10 w-full max-w-[440px]">
            <CornerBrackets />
            <div
              ref={cardRef}
              className={cn(
                "card-spotlight relative rounded-2xl",
                "bg-[oklch(from_var(--color-card)_l_c_h_/_0.78)] backdrop-blur-2xl backdrop-saturate-150",
                "border border-[var(--color-border)]",
                "px-7 pb-6 pt-6 text-left",
                "shadow-[0_30px_60px_-30px_oklch(0_0_0_/_0.30),0_12px_24px_-16px_oklch(0_0_0_/_0.20)]",
              )}
            >
              <div className="mb-5 flex items-center justify-between gap-2 font-mono text-[10.5px] font-medium uppercase tracking-[0.18em]">
                <span className="text-[var(--color-primary)]">// 01.SIGN-IN</span>
                <span className="text-[var(--color-muted-foreground)]">tenant · jwt</span>
              </div>

              <h2 className="text-display pb-1 text-[22px] font-semibold leading-[1.1] tracking-[-0.018em]">
                Welcome <span className="text-gradient-brand">back.</span>
              </h2>
              <p className="mt-1 text-[12.5px] leading-relaxed text-[var(--color-muted-foreground)]">
                Sign in to continue to your tenant.
              </p>

              <form onSubmit={onSubmit} className="mt-5 space-y-3" noValidate>
                <FloatField
                  id="tenant"
                  label="Tenant"
                  value={tenant}
                  onChange={(e) => setTenant(e.target.value)}
                  required
                  autoComplete="organization"
                />
                <FloatField
                  id="email"
                  label="Email"
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  required
                  autoComplete="email"
                />
                <FloatField
                  id="password"
                  label="Password"
                  type="password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  required
                  autoComplete="current-password"
                />

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

                <Button
                  type="submit"
                  size="lg"
                  className="btn-shimmer mt-1.5 w-full"
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
              </form>

              {/* Demo button (DEV only) — opens the popup. Sits as a
                  hairline-divider-separated row at the bottom of the
                  card so it doesn't compete with the primary CTA. */}
              {import.meta.env.DEV && (
                <div className="mt-5 border-t border-[var(--color-border)] pt-4">
                  <button
                    type="button"
                    onClick={() => setDemoOpen(true)}
                    className={cn(
                      "group/demo flex w-full cursor-pointer items-center justify-between gap-2 rounded-md px-2 py-1.5 text-[12px]",
                      "transition-colors duration-[var(--duration-fast)]",
                      "hover:bg-[var(--color-accent)]",
                    )}
                  >
                    <span className="inline-flex items-center gap-2 font-mono uppercase tracking-[0.14em] text-[var(--color-muted-foreground)] group-hover/demo:text-[var(--color-foreground)]">
                      <FlaskConical className="h-3 w-3" />
                      // demo accounts
                    </span>
                    <span className="font-mono text-[10.5px] uppercase tracking-[0.16em] text-[var(--color-muted-foreground)]">
                      DEV
                    </span>
                  </button>
                </div>
              )}
            </div>
          </div>

          {/* Editorial stat strip — three numbers, mono captions,
              hairline dividers. Sits ON the canvas below the card so
              it reads as page-level commentary, not card content. */}
          <div className="fsh-enter fsh-enter-5 mt-10 flex items-stretch gap-7 sm:gap-10">
            <Stat value="14" label="Modules" />
            <Stat value="08" label="Building blocks" border />
            <Stat value="02" label="Demo apps" border />
          </div>

          {/* Trust line — quietly under the stat strip. */}
          <div className="fsh-enter fsh-enter-5 mt-7 inline-flex items-center gap-1.5 text-[11px] tracking-tight text-[var(--color-muted-foreground)]">
            <ShieldCheck className="h-3 w-3" />
            <span>Encrypted in transit · JWT-secured session</span>
          </div>
        </div>
      </main>

      {/* Tech-stack marquee — full-bleed at the bottom. Pauses on hover. */}
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

      {/* DEV demo popup — controlled by the // demo accounts button. */}
      {import.meta.env.DEV && (
        <DemoDialog
          open={demoOpen}
          onOpenChange={setDemoOpen}
          current={{ email, tenant }}
          onSelect={onPickDemo}
        />
      )}
    </div>
  );
}

function Stat({
  value,
  label,
  border,
}: {
  value: string;
  label: string;
  border?: boolean;
}) {
  return (
    <div className={cn(border && "border-l border-[var(--color-border-strong)] pl-7 sm:pl-10")}>
      <div className="text-display text-[32px] font-semibold leading-none tabular-nums tracking-[-0.025em] sm:text-[40px]">
        {value}
      </div>
      <div className="mt-1.5 font-mono text-[10px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
        {label}
      </div>
    </div>
  );
}
