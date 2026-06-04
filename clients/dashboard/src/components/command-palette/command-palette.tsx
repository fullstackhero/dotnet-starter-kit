import {
  createContext,
  lazy,
  Suspense,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";

/**
 * Command palette provider — owns open state + ⌘K / Ctrl+K listener,
 * deliberately tiny so the main bundle stays light. The heavy dialog
 * (cmdk + the full action graph + every lucide icon) lives in a lazy
 * chunk that's fetched the first time the palette opens, so cold start
 * doesn't pay for any of it.
 */

const LazyCommandPaletteDialog = lazy(() =>
  import("./command-palette-dialog").then((mod) => ({
    default: mod.CommandPaletteDialog,
  })),
);

type CommandPaletteContextValue = {
  open: boolean;
  setOpen: (next: boolean) => void;
  toggle: () => void;
};

const CommandPaletteContext = createContext<CommandPaletteContextValue | null>(null);

export function useCommandPalette() {
  const ctx = useContext(CommandPaletteContext);
  if (!ctx) throw new Error("useCommandPalette must be used within CommandPaletteProvider");
  return ctx;
}

/**
 * Owns open state + global keybinding. Mounted at the app root above
 * the router.
 */
export function CommandPaletteProvider({ children }: { children: ReactNode }) {
  const [open, setOpen] = useState(false);
  const toggle = useCallback(() => setOpen((o) => !o), []);

  // Global keybinding — ⌘K on macOS, Ctrl+K elsewhere.
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === "k") {
        e.preventDefault();
        toggle();
      }
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [toggle]);

  const value = useMemo<CommandPaletteContextValue>(
    () => ({ open, setOpen, toggle }),
    [open, toggle],
  );

  return (
    <CommandPaletteContext.Provider value={value}>
      {children}
    </CommandPaletteContext.Provider>
  );
}

/**
 * Mounts the palette dialog. Render this inside the router subtree so
 * useNavigate inside the dialog resolves correctly. The dialog itself
 * is not loaded until the user has opened the palette at least once;
 * after that it stays mounted (cheap when closed) so subsequent opens
 * are instant.
 */
export function CommandPaletteRoot() {
  const { open, setOpen } = useCommandPalette();
  // Track whether the user has ever opened the palette in this session.
  // The lazy dialog is only included in the tree after that first open
  // so the cmdk + lucide-icon bundle deferral actually sticks.
  const [hasEverOpened, setHasEverOpened] = useState(false);
  useEffect(() => {
    if (open) setHasEverOpened(true);
  }, [open]);

  if (!hasEverOpened) return null;

  return (
    <Suspense fallback={null}>
      <LazyCommandPaletteDialog open={open} onOpenChange={setOpen} />
    </Suspense>
  );
}
