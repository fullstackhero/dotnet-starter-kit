import { Link, useLocation, useNavigate } from "react-router-dom";
import { ArrowLeft, Compass, Home, Search } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/cn";

/**
 * NotFoundPage — "lost archive" composition.
 *
 * The aesthetic vocabulary: this page sits *outside* the AppShell (it's
 * mounted at the root via `path: "*"`), so it gets to be its own surface.
 * We borrow login.tsx's atmospheric backdrop (parallax orbs underneath the
 * body's ambient noise) and pair it with a brutalist editorial type
 * treatment — an oversized "404" that reads as a typographic display
 * rather than a status code.
 *
 * The 404 has a "stutter" — a duplicated, offset, low-opacity ghost behind
 * the primary numerals. Reads as a glitched library card, suggests a page
 * that almost exists, doesn't quite.
 *
 * The right column ("the registry") provides return paths: home,
 * back-history, and the requested path itself shown as a mono "lookup
 * receipt" so the user understands *what* was searched.
 */
export function NotFoundPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const requested = location.pathname + location.search;

  return (
    <div className="relative min-h-screen overflow-hidden">
      {/* Atmospheric backdrop — two soft orbs at low opacity. We don't add
          parallax tracking here (this is a terminal page; cursor motion
          would feel decorative-for-its-own-sake). The orbs sit static,
          breathed by the body's global aurora animation. */}
      <div
        aria-hidden
        className="pointer-events-none absolute -left-32 -top-40 h-[520px] w-[520px] rounded-full blur-[140px]"
        style={{ backgroundColor: "oklch(from var(--color-primary) l c h / 0.22)" }}
      />
      <div
        aria-hidden
        className="pointer-events-none absolute -right-40 -bottom-40 h-[560px] w-[560px] rounded-full blur-[160px]"
        style={{ backgroundColor: "oklch(0.700 0.155 195 / 0.18)" }}
      />

      <div className="relative z-10 grid min-h-screen place-items-center px-6 py-16">
        <div className="grid w-full max-w-5xl grid-cols-1 items-center gap-12 lg:grid-cols-[minmax(0,1.1fr)_minmax(0,1fr)] lg:gap-20">
          {/* ─── LEFT — the 404 specimen ──────────────────────────────── */}
          <div className="fsh-enter fsh-enter-1 relative">
            {/* Eyebrow above the numerals */}
            <div className="mb-6 inline-flex items-center gap-2 font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
              <span aria-hidden className="h-px w-6 bg-[var(--color-border-strong)]" />
              FullStackHero · Archive lookup
            </div>

            <NumeralStutter />

            {/* Tagline beneath the numerals */}
            <h1 className="text-display mt-6 text-[28px] font-semibold leading-[1.1] tracking-[-0.02em] sm:text-[32px]">
              We searched the registry —{" "}
              <span className="text-gradient-brand">no card on file.</span>
            </h1>
            <p className="mt-3 max-w-md text-sm leading-relaxed text-[var(--color-muted-foreground)]">
              The address you followed doesn't match any page indexed in this
              tenant's console. Could be a stale link, a renamed route, or a
              path that never existed.
            </p>

            {/* Requested path — mono "lookup receipt" */}
            <div
              className={cn(
                "mt-6 inline-flex max-w-full items-center gap-2.5 rounded-md",
                "border border-dashed border-[var(--color-border)] bg-[var(--color-surface-2)]",
                "px-3 py-2 font-mono text-[12px] text-[var(--color-foreground)]",
                "shadow-[var(--shadow-xs)]",
              )}
              title={requested}
            >
              <Search
                className="h-3.5 w-3.5 shrink-0 text-[var(--color-muted-foreground)]"
                aria-hidden
              />
              <span className="font-mono text-[10px] uppercase tracking-[0.16em] text-[var(--color-muted-foreground)]">
                lookup
              </span>
              <span aria-hidden className="text-[var(--color-border-strong)]">·</span>
              <code className="truncate text-[12px] tracking-tight">{requested}</code>
            </div>
          </div>

          {/* ─── RIGHT — return paths ─────────────────────────────────── */}
          <aside className="fsh-enter fsh-enter-3 relative">
            <div
              className={cn(
                "gradient-border surface-edge relative overflow-hidden rounded-2xl",
                "bg-[oklch(from_var(--color-card)_l_c_h_/_0.72)] backdrop-blur-2xl backdrop-saturate-150",
                "p-6",
              )}
            >
              {/* faint conic accent in the corner */}
              <div
                aria-hidden
                className="pointer-events-none absolute -right-12 -top-12 h-32 w-32 rounded-full opacity-50"
                style={{
                  background:
                    "conic-gradient(from 180deg at 50% 50%, transparent, oklch(from var(--color-primary) l c h / 0.22), transparent)",
                  filter: "blur(20px)",
                }}
              />

              <div className="relative">
                <div className="flex items-center gap-2 font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
                  <Compass className="h-3 w-3" aria-hidden />
                  Find your way
                </div>
                <h2 className="text-display mt-2 text-lg font-semibold tracking-[-0.01em]">
                  Suggested return paths
                </h2>

                <ul className="mt-5 space-y-2.5">
                  <ReturnLink
                    to="/"
                    label="Overview"
                    hint="Tenant dashboard"
                    icon={<Home className="h-3.5 w-3.5" />}
                  />
                  <ReturnLink
                    to="/catalog/products"
                    label="Catalog · Products"
                    hint="Inventory & SKUs"
                    icon={<Search className="h-3.5 w-3.5" />}
                  />
                  <ReturnLink
                    to="/activity"
                    label="Live activity"
                    hint="Realtime event stream"
                    icon={<Compass className="h-3.5 w-3.5" />}
                  />
                </ul>

                <div className="mt-6 flex flex-wrap gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => navigate(-1)}
                    className="gap-1.5"
                  >
                    <ArrowLeft className="h-3.5 w-3.5" />
                    Go back
                  </Button>
                  <Button asChild size="sm" className="brand-glow gradient-sheen gap-1.5">
                    <Link to="/">
                      <Home className="h-3.5 w-3.5" />
                      Take me home
                    </Link>
                  </Button>
                </div>
              </div>
            </div>
          </aside>
        </div>
      </div>

      {/* footer line */}
      <div className="absolute bottom-6 left-1/2 z-10 -translate-x-1/2 font-mono text-[10.5px] uppercase tracking-[0.16em] text-[var(--color-muted-foreground)]">
        404 · Page not found
      </div>
    </div>
  );
}

/**
 * NumeralStutter — the "404" type specimen with a duplicate ghost layer
 * offset behind. The ghost reads at ~0.10 opacity and is offset 6px down,
 * 4px right, creating the impression of a print misregistration.
 *
 * The numerals themselves use a clip-path mask to apply a subtle vertical
 * gradient (foreground → muted) so the bottom edge fades into the page.
 */
function NumeralStutter() {
  return (
    <div className="relative inline-block">
      {/* Ghost layer — duplicated, offset, very faint */}
      <span
        aria-hidden
        className={cn(
          "absolute left-0 top-0 select-none",
          "translate-x-[6px] translate-y-[6px]",
          "text-display font-bold leading-none tracking-[-0.045em]",
          "text-[var(--color-primary)] opacity-[0.10]",
        )}
        style={{ fontSize: "clamp(7rem, 18vw, 15rem)" }}
      >
        404
      </span>
      {/* Primary layer — reads against the ghost */}
      <span
        className={cn(
          "relative select-none",
          "text-display font-bold leading-none tracking-[-0.045em]",
        )}
        style={{
          fontSize: "clamp(7rem, 18vw, 15rem)",
          background:
            "linear-gradient(180deg, var(--color-foreground) 25%, oklch(from var(--color-foreground) l c h / 0.55) 95%)",
          WebkitBackgroundClip: "text",
          backgroundClip: "text",
          WebkitTextFillColor: "transparent",
          color: "transparent",
        }}
      >
        404
      </span>
    </div>
  );
}

/**
 * ReturnLink — a single suggested route in the navigation card. Hovering
 * lifts the row by a hair and shifts the chevron, signaling forward
 * motion without resorting to underline-on-hover noise.
 */
function ReturnLink({
  to,
  label,
  hint,
  icon,
}: {
  to: string;
  label: string;
  hint: string;
  icon: React.ReactNode;
}) {
  return (
    <li>
      <Link
        to={to}
        className={cn(
          "group/link relative flex items-center gap-3 rounded-lg px-3 py-2.5",
          "border border-transparent",
          "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
          "hover:border-[var(--color-border)] hover:bg-[var(--color-surface-2)]",
          "focus-visible:outline-none",
        )}
      >
        <span
          aria-hidden
          className={cn(
            "grid h-7 w-7 place-items-center rounded-md",
            "bg-[var(--color-primary-soft)] text-[var(--color-primary)]",
            "transition-transform duration-[var(--duration-fast)]",
            "group-hover/link:scale-[1.04]",
          )}
        >
          {icon}
        </span>
        <span className="flex flex-1 flex-col">
          <span className="text-sm font-medium leading-tight text-[var(--color-foreground)]">
            {label}
          </span>
          <span className="font-mono text-[10.5px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
            {hint}
          </span>
        </span>
        <span
          aria-hidden
          className={cn(
            "font-mono text-[12px] text-[var(--color-muted-foreground)]",
            "transition-transform duration-[var(--duration-fast)]",
            "group-hover/link:translate-x-0.5 group-hover/link:text-[var(--color-foreground)]",
          )}
        >
          →
        </span>
      </Link>
    </li>
  );
}
