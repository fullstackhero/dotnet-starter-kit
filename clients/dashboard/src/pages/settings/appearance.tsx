import { useEffect, useMemo, useState } from "react";
import { Monitor, Moon, Palette, Sun } from "lucide-react";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  Dialog,
  DialogBody,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Switch } from "@/components/ui/switch";
import { useTheme, type ThemeMode } from "@/components/theme/theme-provider";
import {
  accents,
  buildCustomBrandStops,
  CUSTOM_ACCENT_ID,
  ensureLazyFontsLoaded,
  fonts,
  type AccentOption,
  type CustomAccentSpec,
  type FontOption,
} from "@/components/theme/appearance-options";
import { cn } from "@/lib/cn";

const themeOptions: Array<{
  value: ThemeMode;
  label: string;
  description: string;
  Icon: React.ComponentType<{ className?: string }>;
}> = [
  { value: "light", label: "Light", description: "Bright canvas, day-shift comfort.", Icon: Sun },
  { value: "system", label: "System", description: "Follow the OS preference.", Icon: Monitor },
  { value: "dark", label: "Dark", description: "Reduced glare for long sessions.", Icon: Moon },
];

export function AppearanceSettings() {
  const {
    mode, setMode,
    font, setFont,
    accent, setAccent,
    customAccent, setCustomAccent,
    density, setDensity,
    reducedMotion, setReducedMotion,
  } = useTheme();
  const [customOpen, setCustomOpen] = useState(false);

  // Fetch the nine lazy-loaded selectable font families the first time
  // this page mounts so the font picker swatches render in their own
  // typeface instead of falling back to the system sans.
  useEffect(() => {
    ensureLazyFontsLoaded();
  }, []);

  return (
    <div className="space-y-6 fsh-enter">
      {/* Theme */}
      <Card>
        <CardHeader>
          <CardTitle>Theme</CardTitle>
          <CardDescription>
            Pick a colour mode for the dashboard. System follows your OS.
          </CardDescription>
        </CardHeader>
        <CardContent className="grid gap-3 px-6 pb-5 pt-1 sm:grid-cols-3">
          {themeOptions.map(({ value, label, description, Icon }) => {
            const active = mode === value;
            return (
              <SwatchButton
                key={value}
                active={active}
                onClick={() => setMode(value)}
                aria-pressed={active}
                aria-label={`${label} theme`}
              >
                <div className="mb-3 flex items-center justify-between">
                  <Icon
                    className={cn(
                      "h-4 w-4",
                      active
                        ? "text-[var(--color-primary)]"
                        : "text-[var(--color-muted-foreground)]",
                    )}
                  />
                  {active && <ActiveTag />}
                </div>
                <SwatchTitle active={active}>{label}</SwatchTitle>
                <SwatchSubtitle>{description}</SwatchSubtitle>
              </SwatchButton>
            );
          })}
        </CardContent>
      </Card>

      {/* Accent — six brand palettes */}
      <Card>
        <CardHeader>
          <CardTitle>Accent</CardTitle>
          <CardDescription>
            Pick the brand colour used for primary actions, charts, and
            highlights across the dashboard.
          </CardDescription>
        </CardHeader>
        <CardContent className="grid gap-3 px-6 pb-5 pt-1 sm:grid-cols-3 lg:grid-cols-7">
          {accents.map((a) => (
            <AccentCard
              key={a.id}
              option={a}
              active={accent === a.id}
              onSelect={() => setAccent(a.id)}
            />
          ))}
          {/* Custom accent — opens a hue/chroma picker. */}
          <CustomAccentCard
            active={accent === CUSTOM_ACCENT_ID}
            spec={customAccent}
            onOpen={() => setCustomOpen(true)}
            onActivate={() => setAccent(CUSTOM_ACCENT_ID)}
          />
        </CardContent>
      </Card>

      <CustomAccentDialog
        open={customOpen}
        onOpenChange={setCustomOpen}
        spec={customAccent}
        onApply={(next) => {
          setCustomAccent(next);
          setAccent(CUSTOM_ACCENT_ID);
        }}
      />

      {/* Font — four selectable families */}
      <Card>
        <CardHeader>
          <CardTitle>Font</CardTitle>
          <CardDescription>
            The UI typeface. Mono code blocks always use JetBrains Mono.
          </CardDescription>
        </CardHeader>
        <CardContent className="grid gap-3 px-6 pb-5 pt-1 sm:grid-cols-2 lg:grid-cols-4">
          {fonts.map((f) => (
            <FontCard
              key={f.id}
              option={f}
              active={font === f.id}
              onSelect={() => setFont(f.id)}
            />
          ))}
        </CardContent>
      </Card>

      {/* Density */}
      <Card>
        <CardHeader>
          <CardTitle>Density</CardTitle>
          <CardDescription>
            Compact mode reduces card padding and row height for data-dense screens.
          </CardDescription>
        </CardHeader>
        <CardContent className="flex items-center justify-between gap-4 px-6 pb-5 pt-1">
          <div className="text-sm text-[var(--color-muted-foreground)]">
            Use compact spacing across the dashboard.
          </div>
          <Switch
            checked={density === "compact"}
            onCheckedChange={(checked) => setDensity(checked ? "compact" : "comfortable")}
            aria-label="Compact density"
          />
        </CardContent>
      </Card>

      {/* Motion */}
      <Card>
        <CardHeader>
          <CardTitle>Motion</CardTitle>
          <CardDescription>
            Override the system{" "}
            <code className="rounded bg-[var(--color-muted)] px-1 font-mono text-[11px]">
              prefers-reduced-motion
            </code>{" "}
            setting.
          </CardDescription>
        </CardHeader>
        <CardContent className="flex items-center justify-between gap-4 px-6 pb-5 pt-1">
          <div className="text-sm text-[var(--color-muted-foreground)]">
            Disable transitions and decorative animations.
          </div>
          <Switch
            checked={reducedMotion}
            onCheckedChange={setReducedMotion}
            aria-label="Reduce motion"
          />
        </CardContent>
      </Card>
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
// Building blocks
// ───────────────────────────────────────────────────────────────────────

type SwatchProps = React.ButtonHTMLAttributes<HTMLButtonElement> & {
  active: boolean;
};

function SwatchButton({ active, className, children, ...props }: SwatchProps) {
  return (
    <button
      type="button"
      className={cn(
        "group/swatch relative overflow-hidden rounded-xl border p-4 text-left",
        "transition-colors duration-[var(--duration-default)] ease-[var(--ease-out-cubic)]",
        "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)] focus-visible:ring-offset-2",
        active
          ? "border-[var(--color-primary)] bg-[var(--color-primary-soft)]"
          : "border-[var(--color-border)] bg-[var(--color-card)] hover:bg-[var(--color-muted)]",
        className,
      )}
      {...props}
    >
      {children}
    </button>
  );
}

function ActiveTag() {
  return (
    <span className="text-[10px] font-semibold uppercase tracking-wider text-[var(--color-primary)]">
      Active
    </span>
  );
}

function SwatchTitle({ active, children }: { active: boolean; children: React.ReactNode }) {
  return (
    <div
      className={cn(
        "text-sm font-semibold tracking-tight",
        active && "text-[var(--color-primary)]",
      )}
    >
      {children}
    </div>
  );
}

function SwatchSubtitle({ children }: { children: React.ReactNode }) {
  return (
    <div className="mt-0.5 text-xs leading-relaxed text-[var(--color-muted-foreground)]">
      {children}
    </div>
  );
}

function AccentCard({
  option,
  active,
  onSelect,
}: {
  option: AccentOption;
  active: boolean;
  onSelect: () => void;
}) {
  return (
    <SwatchButton
      active={active}
      onClick={onSelect}
      aria-pressed={active}
      aria-label={`${option.label} accent`}
      title={option.label}
    >
      {/* Two-tone swatch — primary fill on top, soft tint below to
          telegraph the accent's primary-soft variant. */}
      <div
        className="mb-3 h-12 w-full overflow-hidden rounded-lg shadow-[var(--shadow-xs),var(--highlight-top)]"
        style={{
          background: `linear-gradient(180deg, ${option.swatch} 0%, ${option.swatch} 65%, oklch(from ${option.swatch} l c h / 0.18) 65%, oklch(from ${option.swatch} l c h / 0.18) 100%)`,
        }}
        aria-hidden
      />
      <div className="flex items-center justify-between">
        <SwatchTitle active={active}>{option.label}</SwatchTitle>
        {active && <ActiveTag />}
      </div>
      <SwatchSubtitle>{option.description}</SwatchSubtitle>
    </SwatchButton>
  );
}

function FontCard({
  option,
  active,
  onSelect,
}: {
  option: FontOption;
  active: boolean;
  onSelect: () => void;
}) {
  return (
    <SwatchButton
      active={active}
      onClick={onSelect}
      aria-pressed={active}
      aria-label={`${option.label} font`}
      title={option.label}
    >
      {/* Live-preview sample rendered in the candidate font. */}
      <div
        className={cn(
          "mb-3 grid h-12 w-full place-items-center rounded-lg border",
          active
            ? "border-[var(--color-primary)] bg-[oklch(from_var(--color-primary)_l_c_h_/_0.06)]"
            : "border-[var(--color-border)] bg-[var(--color-muted)]",
        )}
        style={{ fontFamily: option.family }}
        aria-hidden
      >
        <span className="font-display text-2xl font-semibold tracking-tight">
          Aa <span className="font-normal text-[var(--color-muted-foreground)]">0123</span>
        </span>
      </div>
      <div className="flex items-center justify-between">
        <SwatchTitle active={active}>{option.label}</SwatchTitle>
        {active && <ActiveTag />}
      </div>
      <SwatchSubtitle>{option.description}</SwatchSubtitle>
    </SwatchButton>
  );
}

// ───────────────────────────────────────────────────────────────────────
// Custom accent — card + picker dialog. The card shows a live two-tone
// swatch derived from the current spec; clicking opens a Dialog with a
// hue ribbon, a chroma slider, and a live preview "screenshot" of
// dashboard chrome rendered in the candidate accent.
// ───────────────────────────────────────────────────────────────────────

function specToSwatch(spec: CustomAccentSpec): string {
  // The 600 stop — what most accent UI sits on. Mirrors the indigo
  // template's L+C and just swaps the hue/chroma scale.
  const c = (0.220 * Math.max(0.4, Math.min(1.4, spec.c))).toFixed(3);
  const h = (((spec.h % 360) + 360) % 360).toFixed(0);
  return `oklch(0.555 ${c} ${h})`;
}

function CustomAccentCard({
  active,
  spec,
  onOpen,
  onActivate,
}: {
  active: boolean;
  spec: CustomAccentSpec;
  onOpen: () => void;
  onActivate: () => void;
}) {
  const swatch = specToSwatch(spec);
  return (
    <SwatchButton
      active={active}
      onClick={() => {
        if (!active) onActivate();
        onOpen();
      }}
      aria-pressed={active}
      aria-label="Custom accent"
      title="Custom accent — click to edit"
    >
      <div
        className="mb-3 h-12 w-full overflow-hidden rounded-lg shadow-[var(--shadow-xs),var(--highlight-top)]"
        style={{
          background: `linear-gradient(180deg, ${swatch} 0%, ${swatch} 65%, oklch(from ${swatch} l c h / 0.18) 65%, oklch(from ${swatch} l c h / 0.18) 100%)`,
        }}
        aria-hidden
      />
      <div className="flex items-center justify-between">
        <span className="inline-flex items-center gap-1.5">
          <Palette className="h-3 w-3 text-[var(--color-muted-foreground)]" aria-hidden />
          <SwatchTitle active={active}>Custom</SwatchTitle>
        </span>
        {active && <ActiveTag />}
      </div>
      <SwatchSubtitle>
        h {Math.round(((spec.h % 360) + 360) % 360)}°
        {" · "}
        c {(spec.c * 100).toFixed(0)}%
      </SwatchSubtitle>
    </SwatchButton>
  );
}

function CustomAccentDialog({
  open,
  onOpenChange,
  spec,
  onApply,
}: {
  open: boolean;
  onOpenChange: (next: boolean) => void;
  spec: CustomAccentSpec;
  onApply: (next: CustomAccentSpec) => void;
}) {
  // Local draft so dragging the slider previews live without committing
  // until Apply. Initialised from the current spec on each open.
  const [draft, setDraft] = useState<CustomAccentSpec>(spec);
  useEffect(() => {
    if (open) setDraft(spec);
  }, [open, spec]);

  // Eleven candidate stops — used to render the live preview ladder.
  const stops = useMemo(() => buildCustomBrandStops(draft), [draft]);
  const swatch600 = specToSwatch(draft);

  // Inline-style overrides on the preview wrapper so the candidate
  // accent paints inside without disturbing the rest of the page.
  const previewStyle = stops.reduce<Record<string, string>>((acc, s) => {
    acc[s.var] = s.value;
    return acc;
  }, {});

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-[560px]">
        <DialogHeader>
          <DialogTitle>Pick your brand colour</DialogTitle>
          <DialogDescription>
            Drag the hue ribbon to recolour the accent. Saturation scales
            chroma uniformly across the eleven brand stops.
          </DialogDescription>
        </DialogHeader>

        <DialogBody className="space-y-5">
          <div className="space-y-1.5">
            <div className="flex items-center justify-between font-mono text-[10.5px] uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
              <span>Hue</span>
              <span className="tabular-nums text-[var(--color-foreground)]">
                {Math.round(draft.h)}°
              </span>
            </div>
            <div
              className="relative h-8 overflow-hidden rounded-lg border border-[var(--color-border)]"
              style={{
                background:
                  "linear-gradient(90deg, oklch(0.62 0.18 0), oklch(0.62 0.18 30), oklch(0.62 0.18 60), oklch(0.62 0.18 90), oklch(0.62 0.18 120), oklch(0.62 0.18 150), oklch(0.62 0.18 180), oklch(0.62 0.18 210), oklch(0.62 0.18 240), oklch(0.62 0.18 270), oklch(0.62 0.18 300), oklch(0.62 0.18 330), oklch(0.62 0.18 360))",
              }}
            >
              <input
                type="range"
                min={0}
                max={360}
                step={1}
                value={Math.round(draft.h)}
                onChange={(e) => setDraft((d) => ({ ...d, h: Number(e.target.value) }))}
                className="absolute inset-0 h-full w-full cursor-pointer appearance-none bg-transparent [&::-webkit-slider-thumb]:h-7 [&::-webkit-slider-thumb]:w-1.5 [&::-webkit-slider-thumb]:appearance-none [&::-webkit-slider-thumb]:rounded-sm [&::-webkit-slider-thumb]:border [&::-webkit-slider-thumb]:border-[oklch(0_0_0_/_0.4)] [&::-webkit-slider-thumb]:bg-[var(--color-overlay-foreground)] [&::-webkit-slider-thumb]:shadow-[0_2px_6px_-2px_oklch(0_0_0_/_0.4)] [&::-moz-range-thumb]:h-7 [&::-moz-range-thumb]:w-1.5 [&::-moz-range-thumb]:rounded-sm [&::-moz-range-thumb]:border [&::-moz-range-thumb]:border-[oklch(0_0_0_/_0.4)] [&::-moz-range-thumb]:bg-[var(--color-overlay-foreground)]"
                aria-label="Hue"
              />
            </div>
          </div>

          <div className="space-y-1.5">
            <div className="flex items-center justify-between font-mono text-[10.5px] uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
              <span>Saturation</span>
              <span className="tabular-nums text-[var(--color-foreground)]">
                {(draft.c * 100).toFixed(0)}%
              </span>
            </div>
            <input
              type="range"
              min={50}
              max={130}
              step={1}
              value={Math.round(draft.c * 100)}
              onChange={(e) =>
                setDraft((d) => ({ ...d, c: Number(e.target.value) / 100 }))
              }
              className="h-2 w-full cursor-pointer appearance-none rounded-full bg-[var(--color-muted)] [&::-webkit-slider-thumb]:h-4 [&::-webkit-slider-thumb]:w-4 [&::-webkit-slider-thumb]:appearance-none [&::-webkit-slider-thumb]:rounded-full [&::-webkit-slider-thumb]:bg-[var(--color-foreground)] [&::-webkit-slider-thumb]:shadow-[0_1px_3px_oklch(0_0_0_/_0.30)] [&::-moz-range-thumb]:h-4 [&::-moz-range-thumb]:w-4 [&::-moz-range-thumb]:rounded-full [&::-moz-range-thumb]:border-0 [&::-moz-range-thumb]:bg-[var(--color-foreground)]"
              aria-label="Saturation"
            />
          </div>

          <div className="space-y-1.5">
            <div className="font-mono text-[10.5px] uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
              Brand ladder
            </div>
            <div className="flex h-7 w-full overflow-hidden rounded-md border border-[var(--color-border)]">
              {stops.map((s) => (
                <div
                  key={s.var}
                  className="flex-1"
                  style={{ background: s.value }}
                  title={`${s.var.replace("--brand-", "")}: ${s.value}`}
                />
              ))}
            </div>
          </div>

          <div className="space-y-1.5">
            <div className="font-mono text-[10.5px] uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
              Preview
            </div>
            <div
              className="rounded-xl border border-[var(--color-border)] bg-[var(--color-muted)] p-4"
              style={previewStyle as React.CSSProperties}
            >
              <div className="flex items-center justify-between">
                <span className="text-[12.5px] font-medium tracking-tight">
                  Subscription <span className="font-mono text-[10.5px] text-[var(--color-muted-foreground)]">· active</span>
                </span>
                <span
                  aria-hidden
                  className="inline-flex h-1.5 w-1.5 rounded-full"
                  style={{ background: swatch600 }}
                />
              </div>
              <div
                className="mt-3 text-2xl font-semibold tracking-tight"
                style={{ color: swatch600 }}
              >
                {Math.round(draft.h)}°
              </div>
              <div className="mt-3 flex items-center gap-2">
                <button
                  type="button"
                  className="rounded-md px-3 py-1.5 text-[11.5px] font-medium text-[var(--color-primary-foreground)] shadow-[var(--highlight-top)]"
                  style={{ background: swatch600 }}
                >
                  Primary action
                </button>
                <code
                  className="rounded px-1.5 py-0.5 font-mono text-[10.5px] font-medium"
                  style={{
                    background: `oklch(from ${swatch600} l c h / 0.10)`,
                    color: swatch600,
                  }}
                >
                  brand-soft
                </code>
              </div>
            </div>
          </div>
        </DialogBody>

        <DialogFooter>
          <Button variant="outline" size="sm" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button
            size="sm"
            onClick={() => {
              onApply(draft);
              onOpenChange(false);
            }}
          >
            Apply accent
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
