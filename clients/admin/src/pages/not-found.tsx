import { Link, useLocation, useNavigate } from "react-router-dom";
import { ArrowLeft, FileQuestion, Home } from "lucide-react";
import { Button } from "@/components/ui/button";

/**
 * NotFoundPage — calm centered "page not found" card.
 *
 * Matches the dashboard's not-found vocabulary: atmospheric orbs, a muted
 * icon tile, an Outfit headline, a soft body line, and primary affordances.
 * Mounted at the root via `path: "*"`.
 */
export function NotFoundPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const requested = location.pathname + location.search;

  return (
    <div className="relative flex min-h-screen items-center justify-center overflow-hidden bg-[var(--color-background)] px-5 py-8 sm:py-12">
      {/* Atmospheric background — same primary/saffron orbs as the auth pages */}
      <div className="pointer-events-none absolute inset-0" aria-hidden>
        <div
          className="absolute -top-[25%] -left-[15%] h-[70vw] w-[70vw] rounded-full blur-[140px]"
          style={{ backgroundColor: "oklch(from var(--color-primary) l c h / 0.05)" }}
        />
        <div
          className="absolute -bottom-[20%] -right-[10%] h-[55vw] w-[55vw] rounded-full blur-[120px]"
          style={{ backgroundColor: "oklch(from var(--color-saffron, var(--color-primary)) l c h / 0.07)" }}
        />
      </div>

      <div className="relative z-10 flex w-full max-w-[460px] flex-col items-center text-center fsh-enter fsh-enter-1">
        {/* Icon tile — muted bg + rounded-2xl, matching dashboard EntityEmpty */}
        <div className="mb-5 grid size-14 place-items-center rounded-2xl bg-[var(--color-muted)]">
          <FileQuestion className="size-6 text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.5)]" />
        </div>

        <h1 className="mb-2 font-display text-[clamp(1.5rem,5vw,2rem)] font-semibold tracking-tight text-[var(--color-foreground)]">
          Page not found
        </h1>
        <p className="mb-2 max-w-[380px] text-[14px] leading-relaxed text-[var(--color-muted-foreground)]">
          We couldn't find anything at that address. It may be a stale link, a
          renamed route, or a path that never existed.
        </p>

        {/* Requested path — soft inline display */}
        <p
          className="mb-7 inline-flex max-w-full items-center gap-1.5 text-[12px] text-[var(--color-muted-foreground)]"
          title={requested}
        >
          <span className="text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.6)]">
            Requested
          </span>
          <span className="truncate text-[var(--color-foreground)]">{requested}</span>
        </p>

        {/* Primary actions */}
        <div className="flex w-full flex-col items-center gap-2 sm:flex-row sm:justify-center">
          <Button asChild className="group h-11 px-5 text-[14px] font-semibold">
            <Link to="/">
              <Home className="size-4" />
              <span>Back home</span>
            </Link>
          </Button>
        </div>

        {/* Tertiary — go-back */}
        <button
          type="button"
          onClick={() => navigate(-1)}
          className="mt-5 inline-flex items-center gap-1.5 text-[12px] text-[var(--color-muted-foreground)] transition-colors hover:text-[var(--color-foreground)]"
        >
          <ArrowLeft className="size-3.5" />
          <span>Go back to the previous page</span>
        </button>
      </div>
    </div>
  );
}
