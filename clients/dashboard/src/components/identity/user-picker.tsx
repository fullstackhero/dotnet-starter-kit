import { useEffect, useMemo, useRef, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Loader2, Search, UserCheck, X } from "lucide-react";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { searchUsers, type UserDto } from "@/api/identity";
import { cn } from "@/lib/cn";

/**
 * UserPicker — debounced typeahead over /identity/users/search. Replaces
 * raw GUID inputs for any "pick a user" affordance (ticket assignment,
 * webhook subscription targeting, etc.).
 *
 * Behaviours:
 * - Selected user renders as a chip above the input, with a clear button.
 * - Typing 1+ characters fires a 250ms-debounced search; results render
 *   in a dropdown beneath the input.
 * - Keyboard: Esc clears the dropdown; clicking outside closes it.
 * - Empty `value` is communicated to the caller via `onChange(null)`.
 *
 * The component does NOT fetch the currently-selected user's display
 * data automatically — the parent passes `initialSelected` if it has
 * the user's name/email handy. Without it, the chip shows the raw id.
 */
export function UserPicker({
  value,
  onChange,
  initialSelected,
  placeholder = "Search by name or email…",
  disabled,
}: {
  value: string | null;
  onChange: (userId: string | null, user: UserDto | null) => void;
  initialSelected?: UserDto | null;
  placeholder?: string;
  disabled?: boolean;
}) {
  const [selected, setSelected] = useState<UserDto | null>(initialSelected ?? null);
  const [query, setQuery] = useState("");
  const [debounced, setDebounced] = useState("");
  const [open, setOpen] = useState(false);
  const wrapperRef = useRef<HTMLDivElement | null>(null);

  // Keep selected snapshot in sync when the parent swaps the value.
  // We also sync when the parent passes a fresh initialSelected after
  // having loaded a ticket / row by id.
  useEffect(() => {
    if (initialSelected && initialSelected.id === value) {
      setSelected(initialSelected);
    } else if (value === null) {
      setSelected(null);
    }
  }, [value, initialSelected]);

  // Debounce the query string — typing fires the search 250ms after the
  // last keystroke, so a fast typist doesn't generate ten in-flight calls.
  useEffect(() => {
    const t = setTimeout(() => setDebounced(query.trim()), 250);
    return () => clearTimeout(t);
  }, [query]);

  // Close on outside click.
  useEffect(() => {
    if (!open) return undefined;
    const onDown = (e: MouseEvent) => {
      if (!wrapperRef.current?.contains(e.target as Node)) {
        setOpen(false);
      }
    };
    document.addEventListener("mousedown", onDown);
    return () => document.removeEventListener("mousedown", onDown);
  }, [open]);

  const resultsQuery = useQuery({
    queryKey: ["identity", "users", "search", debounced],
    queryFn: () => searchUsers({ search: debounced, pageNumber: 1, pageSize: 8 }),
    enabled: debounced.length > 0 && open,
    staleTime: 30_000,
  });

  const results = useMemo(
    () => resultsQuery.data?.items.filter((u) => !!u.id) ?? [],
    [resultsQuery.data],
  );

  const pick = (user: UserDto) => {
    if (!user.id) return;
    setSelected(user);
    setQuery("");
    setOpen(false);
    onChange(user.id, user);
  };

  const clear = () => {
    setSelected(null);
    setQuery("");
    setOpen(false);
    onChange(null, null);
  };

  return (
    <div ref={wrapperRef} className="space-y-2">
      {selected && (
        <div className="flex items-center justify-between gap-2 rounded-md border border-[var(--color-border)] bg-[var(--color-muted)] px-3 py-2">
          <div className="flex min-w-0 items-center gap-2.5">
            <span
              aria-hidden
              className="grid h-7 w-7 shrink-0 place-items-center rounded-full bg-[var(--color-primary-soft)] text-[var(--color-primary)]"
            >
              <UserCheck className="h-3.5 w-3.5" />
            </span>
            <div className="min-w-0">
              <div className="truncate text-sm font-medium tracking-tight">
                {[selected.firstName, selected.lastName].filter(Boolean).join(" ") ||
                  selected.userName ||
                  selected.id}
              </div>
              {selected.email && (
                <div className="truncate font-mono text-[11px] text-[var(--color-muted-foreground)]">
                  {selected.email}
                </div>
              )}
            </div>
          </div>
          {!disabled && (
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={clear}
              aria-label="Clear selection"
              className="shrink-0"
            >
              <X className="h-3.5 w-3.5" />
              Clear
            </Button>
          )}
        </div>
      )}

      <div className="relative">
        <Search
          aria-hidden
          className="pointer-events-none absolute left-2.5 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-[var(--color-muted-foreground)]"
        />
        <Input
          type="search"
          placeholder={selected ? "Search to reassign…" : placeholder}
          value={query}
          onChange={(e) => {
            setQuery(e.target.value);
            setOpen(true);
          }}
          onFocus={() => setOpen(true)}
          onKeyDown={(e) => {
            if (e.key === "Escape") {
              setOpen(false);
              e.currentTarget.blur();
            }
          }}
          autoComplete="off"
          spellCheck={false}
          disabled={disabled}
          className="pl-8"
        />

        {open && debounced.length > 0 && (
          <div className="absolute left-0 right-0 top-full z-30 mt-1 max-h-64 overflow-auto rounded-md border border-[var(--color-border)] bg-[var(--color-card)] shadow-[var(--shadow-lift)]">
            {resultsQuery.isFetching && results.length === 0 ? (
              <div className="flex items-center gap-2 px-3 py-2.5 text-[12.5px] text-[var(--color-muted-foreground)]">
                <Loader2 className="h-3.5 w-3.5 animate-spin" />
                Searching for "{debounced}"…
              </div>
            ) : results.length === 0 ? (
              <div className="px-3 py-3 text-center text-[12.5px] text-[var(--color-muted-foreground)]">
                No users match "{debounced}".
              </div>
            ) : (
              <div role="listbox" aria-label="Search results">
                {results.map((u) => (
                  <div key={u.id}>
                    <button
                      type="button"
                      role="option"
                      aria-selected={u.id === value}
                      onClick={() => pick(u)}
                      className={cn(
                        "flex w-full items-center gap-2.5 px-3 py-2 text-left",
                        "transition-colors",
                        "hover:bg-[var(--color-muted)] focus-visible:bg-[var(--color-muted)] focus-visible:outline-none",
                        u.id === value && "bg-[var(--color-primary-soft)]",
                      )}
                    >
                      <span
                        aria-hidden
                        className="grid h-6 w-6 shrink-0 place-items-center rounded-full bg-[var(--color-muted)] text-[var(--color-muted-foreground)]"
                      >
                        {(u.firstName?.[0] ?? u.userName?.[0] ?? "?").toUpperCase()}
                      </span>
                      <div className="min-w-0 flex-1">
                        <div className="truncate text-[13px] font-medium tracking-tight">
                          {[u.firstName, u.lastName].filter(Boolean).join(" ") ||
                            u.userName ||
                            u.id}
                        </div>
                        <div className="truncate font-mono text-[10.5px] text-[var(--color-muted-foreground)]">
                          {u.email ?? u.id}
                        </div>
                      </div>
                    </button>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
