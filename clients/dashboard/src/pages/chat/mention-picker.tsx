import { AtSign } from "lucide-react";
import type { UserDto } from "@/api/identity";
import { Avatar } from "@/components/ui/avatar";
import { cn } from "@/lib/cn";

/**
 * Floating autocomplete list rendered above the composer when the caller has
 * typed `@<2+ chars>`. The host (Composer) owns keyboard navigation and
 * selection — this component just renders the list and surfaces hover-as-
 * highlight so mouse + keyboard stay in sync.
 *
 * The list is always rooted at the composer, not the document — Sonner et al.
 * portal their UI but a mention picker is part of the text-input contract
 * and shouldn't escape the surface that owns the caret.
 */
export function MentionPicker({
  candidates,
  highlight,
  onHighlight,
  onSelect,
  loading,
  className,
}: {
  candidates: UserDto[];
  highlight: number;
  onHighlight: (index: number) => void;
  onSelect: (user: UserDto) => void;
  loading: boolean;
  className?: string;
}) {
  return (
    <div
      role="listbox"
      aria-label="Mention suggestions"
      className={cn(
        "z-20 rounded-xl border bg-[var(--color-popover)] shadow-[var(--shadow-lift)]",
        "border-[var(--color-border)]",
        className,
      )}
    >
      <div className="flex items-center gap-1.5 border-b border-[var(--color-border)] px-3 py-1.5">
        <AtSign className="h-3 w-3 text-[var(--color-muted-foreground)]" aria-hidden />
        <span className="text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
          Mention
        </span>
        <span className="ml-auto text-[11px] text-[var(--color-muted-foreground)]">
          ↑↓ navigate · Enter select · Esc cancel
        </span>
      </div>
      <ul className="max-h-[240px] overflow-y-auto p-1">
        {candidates.map((user, i) => {
          const display = renderName(user);
          const isHighlighted = i === highlight;
          return (
            <li key={user.id ?? `${i}-${display}`}>
              <button
                type="button"
                role="option"
                aria-selected={isHighlighted}
                onMouseEnter={() => onHighlight(i)}
                // Prevent the textarea from losing focus when clicking a
                // suggestion — onMouseDown fires before blur.
                onMouseDown={(e) => e.preventDefault()}
                onClick={() => onSelect(user)}
                className={cn(
                  "flex w-full cursor-pointer items-center gap-2.5 rounded-md p-2 text-left",
                  "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
                  isHighlighted
                    ? "bg-[var(--color-primary-soft)] text-[var(--color-primary)]"
                    : "hover:bg-[var(--color-accent)]",
                )}
              >
                <Avatar name={display} src={user.imageUrl ?? null} size="sm" />
                <div className="min-w-0 flex-1">
                  <div className="flex items-baseline gap-1.5">
                    <span className="truncate text-sm font-medium tracking-tight text-[var(--color-foreground)]">
                      {display}
                    </span>
                    {user.userName && (
                      <span className="truncate text-[11px] text-[var(--color-muted-foreground)]">
                        @{user.userName}
                      </span>
                    )}
                  </div>
                  {user.email && (
                    <div className="truncate text-[11px] text-[var(--color-muted-foreground)]">
                      {user.email}
                    </div>
                  )}
                </div>
              </button>
            </li>
          );
        })}
      </ul>
      {loading && (
        <div className="border-t border-[var(--color-border)] px-3 py-1.5">
          <span className="text-[11px] text-[var(--color-muted-foreground)]">
            Searching…
          </span>
        </div>
      )}
    </div>
  );
}

function renderName(u: UserDto): string {
  const full = [u.firstName, u.lastName].filter(Boolean).join(" ").trim();
  return full || u.userName || u.email || "(unnamed)";
}
