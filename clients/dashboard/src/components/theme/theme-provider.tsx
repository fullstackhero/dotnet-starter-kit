import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { flushSync } from "react-dom";
import {
  ACCENT_STORAGE_KEY,
  accents,
  buildCustomBrandStops,
  CUSTOM_ACCENT_ID,
  CUSTOM_ACCENT_STORAGE_KEY,
  DEFAULT_ACCENT,
  DEFAULT_CUSTOM_ACCENT,
  DEFAULT_DENSITY,
  DEFAULT_FONT,
  DENSITY_STORAGE_KEY,
  FONT_STORAGE_KEY,
  fonts,
  type CustomAccentSpec,
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
  /** Currently configured custom accent spec — drives the live preview
   *  in the appearance UI even when a preset is selected. */
  customAccent: CustomAccentSpec;
  setCustomAccent: (spec: CustomAccentSpec) => void;
  density: DensityMode;
  setDensity: (next: DensityMode) => void;
};

const ThemeContext = createContext<ThemeContextValue | null>(null);
const THEME_STORAGE_KEY = "fsh.theme";
const ACCENT_CLASS_PREFIX = "accent-";
const FALLBACK_TRANSITION_MS = 280;

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

function applyDarkClass(next: ResolvedTheme) {
  document.documentElement.classList.toggle("dark", next === "dark");
}

let fallbackTimer: number | undefined;

/**
 * Wraps a DOM mutation in the smoothest available crossfade:
 *
 *  1. View Transitions API — single bitmap snapshot crossfade. The only
 *     mechanism that can morph gradients, box-shadows, SVG fills,
 *     conic/radial backgrounds, and image-based surfaces in lockstep
 *     with text and bg-color. Handles every property uniformly.
 *  2. Firefox / older browsers — opt into the scoped `theme-switching`
 *     blanket transition for the subset of properties CSS *can*
 *     interpolate.
 *  3. Reduced-motion / cold load — apply instantly, no animation.
 *
 * The commit callback MUST update the DOM synchronously. We use
 * `flushSync` at the call site so React's state updates land inside the
 * View Transitions snapshot window — otherwise the toggle thumb would
 * lead the actual class flip by a frame and the snapshot would be
 * inconsistent with the new state.
 */
function withThemeTransition(commit: () => void): void {
  const root = document.documentElement;
  const ready = root.classList.contains("theme-ready");
  const reduceMotion =
    typeof window !== "undefined" &&
    window.matchMedia("(prefers-reduced-motion: reduce)").matches;
  const canViewTransition = typeof document.startViewTransition === "function";

  if (!ready || reduceMotion) {
    commit();
    return;
  }

  if (canViewTransition) {
    // Mark the document for the duration of the transition so component
    // CSS can opt out of competing transitions (e.g. the seg-thumb's own
    // transform animation, which would otherwise interpolate inside the
    // captured snapshot and tear).
    root.classList.add("vt-active");
    const transition = document.startViewTransition!(commit);
    const cleanup = () => root.classList.remove("vt-active");
    transition.finished.then(cleanup, cleanup);
    return;
  }

  root.classList.add("theme-switching");
  if (fallbackTimer !== undefined) window.clearTimeout(fallbackTimer);
  try {
    commit();
  } finally {
    fallbackTimer = window.setTimeout(() => {
      root.classList.remove("theme-switching");
      fallbackTimer = undefined;
    }, FALLBACK_TRANSITION_MS);
  }
}

function applyFont(id: string) {
  if (typeof document === "undefined") return;
  const opt = fonts.find((f) => f.id === id) ?? fonts[0];
  document.documentElement.style.setProperty("--font-sans", opt.family);
}

const BRAND_VARS: ReadonlyArray<string> = [
  "--brand-50", "--brand-100", "--brand-200", "--brand-300", "--brand-400",
  "--brand-500", "--brand-600", "--brand-700", "--brand-800", "--brand-900",
  "--brand-950",
];

function clearCustomBrandInlineStyles(root: HTMLElement) {
  for (const v of BRAND_VARS) root.style.removeProperty(v);
}

function applyCustomBrandInlineStyles(root: HTMLElement, spec: CustomAccentSpec) {
  for (const { var: name, value } of buildCustomBrandStops(spec)) {
    root.style.setProperty(name, value);
  }
}

function applyAccent(id: string, customSpec: CustomAccentSpec) {
  if (typeof document === "undefined") return;
  const root = document.documentElement;
  // Strip any existing accent-* class then add the new one. The default
  // (indigo) lives in :root so it needs no class.
  root.className = root.className
    .split(/\s+/)
    .filter((c) => !c.startsWith(ACCENT_CLASS_PREFIX))
    .join(" ");

  if (id === CUSTOM_ACCENT_ID) {
    // Custom accent — inline-style the eleven --brand-* stops from the
    // current spec. Inline styles win over the accent-* class rules so
    // we don't need to add any class.
    applyCustomBrandInlineStyles(root, customSpec);
    return;
  }

  // Preset (or default). Make sure inline overrides aren't lingering
  // from a previous custom selection — otherwise the chosen preset
  // would be invisible.
  clearCustomBrandInlineStyles(root);

  if (id !== DEFAULT_ACCENT && accents.some((a) => a.id === id)) {
    root.classList.add(`${ACCENT_CLASS_PREFIX}${id}`);
  }
}

function readStoredCustomAccent(): CustomAccentSpec {
  if (typeof window === "undefined") return DEFAULT_CUSTOM_ACCENT;
  try {
    const raw = window.localStorage.getItem(CUSTOM_ACCENT_STORAGE_KEY);
    if (!raw) return DEFAULT_CUSTOM_ACCENT;
    const parsed = JSON.parse(raw) as Partial<CustomAccentSpec>;
    return {
      h: typeof parsed.h === "number" ? parsed.h : DEFAULT_CUSTOM_ACCENT.h,
      c: typeof parsed.c === "number" ? parsed.c : DEFAULT_CUSTOM_ACCENT.c,
    };
  } catch {
    return DEFAULT_CUSTOM_ACCENT;
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
  const [customAccent, setCustomAccentState] = useState<CustomAccentSpec>(() =>
    readStoredCustomAccent(),
  );
  const [density, setDensityState] = useState<DensityMode>(() => {
    const stored = readStoredString(DENSITY_STORAGE_KEY, DEFAULT_DENSITY);
    return stored === "compact" ? "compact" : DEFAULT_DENSITY;
  });

  // Apply font / accent / density — covers initial render and any
  // subsequent change. The dark class is owned by withThemeTransition
  // and the index.html bootstrap script; no useEffect for `resolved`.
  // Custom-accent application also re-runs whenever `customAccent`
  // changes so a hue tweak previews live.
  useEffect(() => applyFont(font), [font]);
  useEffect(() => applyAccent(accent, customAccent), [accent, customAccent]);
  useEffect(() => applyDensity(density), [density]);

  // Subscribe to system preference while in "system" mode. Future OS
  // changes route through withThemeTransition so they crossfade too.
  useEffect(() => {
    if (mode !== "system") return undefined;
    const mq = window.matchMedia("(prefers-color-scheme: dark)");
    const update = () => {
      const next: ResolvedTheme = mq.matches ? "dark" : "light";
      withThemeTransition(() => {
        flushSync(() => setResolved(next));
        applyDarkClass(next);
      });
    };
    mq.addEventListener("change", update);
    return () => mq.removeEventListener("change", update);
  }, [mode]);

  const setMode = useCallback((next: ThemeMode) => {
    const nextResolved: ResolvedTheme =
      next === "dark"
        ? "dark"
        : next === "light"
          ? "light"
          : systemPrefersDark()
            ? "dark"
            : "light";

    withThemeTransition(() => {
      // flushSync ensures the new mode/resolved render lands BEFORE the
      // View Transitions API captures the new snapshot — otherwise the
      // thumb position in the new snapshot wouldn't match the new state.
      flushSync(() => {
        setModeState(next);
        setResolved(nextResolved);
      });
      applyDarkClass(nextResolved);
    });

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

  const setCustomAccent = useCallback((spec: CustomAccentSpec) => {
    setCustomAccentState(spec);
    try {
      window.localStorage.setItem(CUSTOM_ACCENT_STORAGE_KEY, JSON.stringify(spec));
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
    () => ({
      mode, resolved, setMode,
      font, setFont,
      accent, setAccent,
      customAccent, setCustomAccent,
      density, setDensity,
    }),
    [
      mode, resolved, setMode,
      font, setFont,
      accent, setAccent,
      customAccent, setCustomAccent,
      density, setDensity,
    ],
  );

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}

export function useTheme() {
  const ctx = useContext(ThemeContext);
  if (!ctx) throw new Error("useTheme must be used within ThemeProvider");
  return ctx;
}
