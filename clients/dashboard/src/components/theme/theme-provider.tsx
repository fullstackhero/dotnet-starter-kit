import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import {
  ACCENT_STORAGE_KEY,
  accents,
  DEFAULT_ACCENT,
  DEFAULT_DENSITY,
  DEFAULT_FONT,
  DENSITY_STORAGE_KEY,
  FONT_STORAGE_KEY,
  fonts,
  type DensityMode,
} from "@/components/theme/appearance-options";

export type ThemeMode = "light" | "dark" | "system";
type ResolvedTheme = "light" | "dark";

type ThemeContextValue = {
  mode: ThemeMode;
  resolved: ResolvedTheme;
  setMode: (mode: ThemeMode) => void;
  font: string;
  setFont: (id: string) => void;
  accent: string;
  setAccent: (id: string) => void;
  density: DensityMode;
  setDensity: (next: DensityMode) => void;
};

const ThemeContext = createContext<ThemeContextValue | null>(null);
const THEME_STORAGE_KEY = "fsh.theme";
const ACCENT_CLASS_PREFIX = "accent-";

function readStoredMode(): ThemeMode {
  if (typeof window === "undefined") return "system";
  try {
    const stored = window.localStorage.getItem(THEME_STORAGE_KEY);
    return stored === "light" || stored === "dark" || stored === "system" ? stored : "system";
  } catch {
    return "system";
  }
}

function readStoredString(key: string, fallback: string): string {
  if (typeof window === "undefined") return fallback;
  try {
    return window.localStorage.getItem(key) ?? fallback;
  } catch {
    return fallback;
  }
}

function systemPrefersDark(): boolean {
  if (typeof window === "undefined") return false;
  return window.matchMedia("(prefers-color-scheme: dark)").matches;
}

function applyResolved(resolved: ResolvedTheme) {
  const root = document.documentElement;
  root.classList.toggle("dark", resolved === "dark");
}

function applyFont(id: string) {
  if (typeof document === "undefined") return;
  const opt = fonts.find((f) => f.id === id) ?? fonts[0];
  document.documentElement.style.setProperty("--font-sans", opt.family);
}

function applyAccent(id: string) {
  if (typeof document === "undefined") return;
  const root = document.documentElement;
  // Strip any existing accent-* class then add the new one. The default
  // (indigo) lives in :root so it needs no class.
  root.className = root.className
    .split(/\s+/)
    .filter((c) => !c.startsWith(ACCENT_CLASS_PREFIX))
    .join(" ");
  if (id !== DEFAULT_ACCENT && accents.some((a) => a.id === id)) {
    root.classList.add(`${ACCENT_CLASS_PREFIX}${id}`);
  }
}

function applyDensity(value: DensityMode) {
  if (typeof document === "undefined") return;
  document.documentElement.classList.toggle("density-compact", value === "compact");
}

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [mode, setModeState] = useState<ThemeMode>(() => readStoredMode());
  const [resolved, setResolved] = useState<ResolvedTheme>(() =>
    readStoredMode() === "dark"
      ? "dark"
      : readStoredMode() === "light"
        ? "light"
        : systemPrefersDark()
          ? "dark"
          : "light",
  );
  const [font, setFontState] = useState<string>(() =>
    readStoredString(FONT_STORAGE_KEY, DEFAULT_FONT),
  );
  const [accent, setAccentState] = useState<string>(() =>
    readStoredString(ACCENT_STORAGE_KEY, DEFAULT_ACCENT),
  );
  const [density, setDensityState] = useState<DensityMode>(() => {
    const stored = readStoredString(DENSITY_STORAGE_KEY, DEFAULT_DENSITY);
    return stored === "compact" ? "compact" : DEFAULT_DENSITY;
  });

  // Apply resolved theme on every change.
  useEffect(() => {
    applyResolved(resolved);
  }, [resolved]);

  // Apply font / accent / density — covers initial render and any
  // subsequent change.
  useEffect(() => applyFont(font), [font]);
  useEffect(() => applyAccent(accent), [accent]);
  useEffect(() => applyDensity(density), [density]);

  // Recompute resolved when mode changes; subscribe to system in "system" mode.
  useEffect(() => {
    if (mode === "system") {
      const mq = window.matchMedia("(prefers-color-scheme: dark)");
      const update = () => setResolved(mq.matches ? "dark" : "light");
      update();
      mq.addEventListener("change", update);
      return () => mq.removeEventListener("change", update);
    }
    setResolved(mode);
    return undefined;
  }, [mode]);

  const setMode = useCallback((next: ThemeMode) => {
    setModeState(next);
    try {
      window.localStorage.setItem(THEME_STORAGE_KEY, next);
    } catch {
      /* storage unavailable */
    }
  }, []);

  const setFont = useCallback((id: string) => {
    setFontState(id);
    try {
      window.localStorage.setItem(FONT_STORAGE_KEY, id);
    } catch {
      /* storage unavailable */
    }
  }, []);

  const setAccent = useCallback((id: string) => {
    setAccentState(id);
    try {
      window.localStorage.setItem(ACCENT_STORAGE_KEY, id);
    } catch {
      /* storage unavailable */
    }
  }, []);

  const setDensity = useCallback((next: DensityMode) => {
    setDensityState(next);
    try {
      window.localStorage.setItem(DENSITY_STORAGE_KEY, next);
    } catch {
      /* storage unavailable */
    }
  }, []);

  const value = useMemo<ThemeContextValue>(
    () => ({ mode, resolved, setMode, font, setFont, accent, setAccent, density, setDensity }),
    [mode, resolved, setMode, font, setFont, accent, setAccent, density, setDensity],
  );

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}

export function useTheme() {
  const ctx = useContext(ThemeContext);
  if (!ctx) throw new Error("useTheme must be used within ThemeProvider");
  return ctx;
}
