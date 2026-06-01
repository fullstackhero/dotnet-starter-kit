/**
 * Selectable appearance options — fonts and accent palettes.
 *
 * Fonts are applied by setting `--font-sans` on :root.
 * Accents are applied by toggling a single `accent-{id}` class on :root;
 * the class definitions live in globals.css and override the eleven
 * `--brand-*` stops the entire token system reads from.
 *
 * Storage keys are namespaced under `fsh.admin.*` so the admin app's
 * appearance prefs never collide with the dashboard app's `fsh.*` keys.
 */

export type FontOption = {
  id: string;
  label: string;
  description: string;
  /** CSS font-family value applied to --font-sans. */
  family: string;
  /** Sample shown on the swatch card. */
  sample?: string;
};

const SHARED_FALLBACKS =
  "ui-sans-serif, system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif";

export const fonts: FontOption[] = [
  {
    id: "geist",
    label: "Geist",
    description: "Designed for screens. Default.",
    family: `'Geist', ${SHARED_FALLBACKS}`,
  },
  {
    id: "inter-tight",
    label: "Inter Tight",
    description: "Tighter modern Inter.",
    family: `'Inter Tight', ${SHARED_FALLBACKS}`,
  },
  {
    id: "dm-sans",
    label: "DM Sans",
    description: "Geometric, friendly.",
    family: `'DM Sans', ${SHARED_FALLBACKS}`,
  },
  {
    id: "ibm-plex",
    label: "IBM Plex Sans",
    description: "Editorial grotesque.",
    family: `'IBM Plex Sans', ${SHARED_FALLBACKS}`,
  },
  {
    id: "manrope",
    label: "Manrope",
    description: "Warm, geometric.",
    family: `'Manrope', ${SHARED_FALLBACKS}`,
  },
  {
    id: "plus-jakarta",
    label: "Plus Jakarta Sans",
    description: "Modern, lightly geometric.",
    family: `'Plus Jakarta Sans', ${SHARED_FALLBACKS}`,
  },
  {
    id: "outfit",
    label: "Outfit",
    description: "Confident geometric sans.",
    family: `'Outfit', ${SHARED_FALLBACKS}`,
  },
  {
    id: "sora",
    label: "Sora",
    description: "Distinctive, contemporary.",
    family: `'Sora', ${SHARED_FALLBACKS}`,
  },
  {
    id: "lexend",
    label: "Lexend",
    description: "Tuned for reading speed.",
    family: `'Lexend', ${SHARED_FALLBACKS}`,
  },
  {
    id: "figtree",
    label: "Figtree",
    description: "Friendly, approachable.",
    family: `'Figtree', ${SHARED_FALLBACKS}`,
  },
  {
    id: "onest",
    label: "Onest",
    description: "Clean, neutral grotesque.",
    family: `'Onest', ${SHARED_FALLBACKS}`,
  },
  {
    id: "roboto-flex",
    label: "Roboto Flex",
    description: "Google's flagship variable.",
    family: `'Roboto Flex', ${SHARED_FALLBACKS}`,
  },
];

export const DEFAULT_FONT = "figtree";

export type AccentOption = {
  id: string;
  label: string;
  description: string;
  /** Display-only OKLCH at the 600 stop, used to colour swatch chips. */
  swatch: string;
};

export const accents: AccentOption[] = [
  { id: "rose",    label: "Rose",    description: "Editorial, warm-paper default.",  swatch: "oklch(0.575 0.232  13)" },
  { id: "indigo",  label: "Indigo",  description: "Confident tech-forward chassis.", swatch: "oklch(0.555 0.220 268)" },
  { id: "violet",  label: "Violet",  description: "Saturated, expressive.",          swatch: "oklch(0.555 0.220 305)" },
  { id: "sky",     label: "Sky",     description: "Cool, calm, professional.",       swatch: "oklch(0.555 0.220 232)" },
  { id: "emerald", label: "Emerald", description: "Fresh, success-leaning.",         swatch: "oklch(0.555 0.220 152)" },
  { id: "amber",   label: "Amber",   description: "Warm, energetic.",                swatch: "oklch(0.620 0.180  76)" },
];

export const DEFAULT_ACCENT = "rose";
export const CUSTOM_ACCENT_ID = "custom";

export const FONT_STORAGE_KEY = "fsh.admin.font";
export const ACCENT_STORAGE_KEY = "fsh.admin.accent";
/** Stores `{ h, c }` for the custom accent — h = hue (0-360),
 *  c = chroma scale (0.6 → 1.2 of the indigo template). */
export const CUSTOM_ACCENT_STORAGE_KEY = "fsh.admin.accent.custom";

export type DensityMode = "comfortable" | "compact";
export const DENSITY_STORAGE_KEY = "fsh.admin.density";
export const DEFAULT_DENSITY: DensityMode = "comfortable";

// ────────────────────────────────────────────────────────────────────────
// Custom accent — derive the eleven --brand-* stops from a single hue.
//
// The (lightness, chroma) ladder mirrors the indigo template in
// globals.css. For a custom accent we keep L + C constant and vary only
// H, then optionally scale chroma uniformly so users can dial down or
// pump up saturation.
// ────────────────────────────────────────────────────────────────────────

type BrandStop = { stop: number; l: number; c: number };

const BRAND_LADDER: ReadonlyArray<BrandStop> = [
  { stop:  50, l: 0.972, c: 0.020 },
  { stop: 100, l: 0.945, c: 0.040 },
  { stop: 200, l: 0.895, c: 0.078 },
  { stop: 300, l: 0.825, c: 0.130 },
  { stop: 400, l: 0.720, c: 0.180 },
  { stop: 500, l: 0.620, c: 0.210 },
  { stop: 600, l: 0.555, c: 0.220 },
  { stop: 700, l: 0.485, c: 0.205 },
  { stop: 800, l: 0.405, c: 0.175 },
  { stop: 900, l: 0.325, c: 0.135 },
  { stop: 950, l: 0.230, c: 0.090 },
];

export type CustomAccentSpec = {
  /** Hue in degrees, 0-360. */
  h: number;
  /** Chroma multiplier — 1.0 == indigo template intensity. */
  c: number;
};

export const DEFAULT_CUSTOM_ACCENT: CustomAccentSpec = { h: 12, c: 1.0 };

// ────────────────────────────────────────────────────────────────────────
// Lazy fonts — the index.html boot only loads Figtree + Outfit +
// JetBrains Mono so cold start stays cheap. The other nine selectable
// families are fetched on demand the first time the user opens the
// Appearance settings (where their swatches need to render correctly).
// Idempotent: a second call is a no-op.
// ────────────────────────────────────────────────────────────────────────

const LAZY_FONTS_HREF =
  "https://fonts.googleapis.com/css2?" +
  [
    "family=DM+Sans:opsz,wght@9..40,100..1000",
    "family=Geist:wght@100..900",
    "family=IBM+Plex+Sans:wght@100;200;300;400;500;600;700",
    "family=Inter+Tight:wght@100..900",
    "family=Lexend:wght@100..900",
    "family=Manrope:wght@200..800",
    "family=Onest:wght@100..900",
    "family=Plus+Jakarta+Sans:wght@200..800",
    "family=Roboto+Flex:opsz,wght@8..144,100..1000",
    "family=Sora:wght@100..800",
    "display=swap",
  ].join("&");

const LAZY_FONTS_LINK_ID = "fsh-lazy-fonts";

export function ensureLazyFontsLoaded(): void {
  if (typeof document === "undefined") return;
  if (document.getElementById(LAZY_FONTS_LINK_ID)) return;
  const link = document.createElement("link");
  link.id = LAZY_FONTS_LINK_ID;
  link.rel = "stylesheet";
  link.href = LAZY_FONTS_HREF;
  document.head.appendChild(link);
}

/** Build the eleven `--brand-*` value strings for a given custom spec. */
export function buildCustomBrandStops(
  spec: CustomAccentSpec,
): ReadonlyArray<{ var: string; value: string }> {
  const h = ((spec.h % 360) + 360) % 360;
  const cScale = Math.max(0.4, Math.min(1.4, spec.c));
  return BRAND_LADDER.map(({ stop, l, c }) => ({
    var: `--brand-${stop}`,
    value: `oklch(${l.toFixed(3)} ${(c * cScale).toFixed(3)} ${h.toFixed(0)})`,
  }));
}
