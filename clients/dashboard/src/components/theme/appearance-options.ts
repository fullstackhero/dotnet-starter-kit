/**
 * Selectable appearance options — fonts and accent palettes.
 *
 * Fonts are applied by setting `--font-sans` on :root.
 * Accents are applied by toggling a single `accent-{id}` class on :root;
 * the class definitions live in globals.css and override the eleven
 * `--brand-*` stops the entire token system reads from.
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

export const DEFAULT_FONT = "geist";

export type AccentOption = {
  id: string;
  label: string;
  description: string;
  /** Display-only OKLCH at the 600 stop, used to colour swatch chips. */
  swatch: string;
};

export const accents: AccentOption[] = [
  { id: "indigo",  label: "Indigo",  description: "Confident tech-forward default.", swatch: "oklch(0.555 0.220 268)" },
  { id: "violet",  label: "Violet",  description: "Saturated, expressive.",          swatch: "oklch(0.555 0.220 305)" },
  { id: "sky",     label: "Sky",     description: "Cool, calm, professional.",       swatch: "oklch(0.555 0.220 232)" },
  { id: "emerald", label: "Emerald", description: "Fresh, success-leaning.",         swatch: "oklch(0.555 0.220 152)" },
  { id: "amber",   label: "Amber",   description: "Warm, energetic.",                swatch: "oklch(0.620 0.180  76)" },
  { id: "rose",    label: "Rose",    description: "Bold, attention-getting.",        swatch: "oklch(0.555 0.220  12)" },
];

export const DEFAULT_ACCENT = "indigo";

export const FONT_STORAGE_KEY = "fsh.font";
export const ACCENT_STORAGE_KEY = "fsh.accent";
