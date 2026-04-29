import { useState } from "react";
import { Monitor, Moon, Sun } from "lucide-react";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Switch } from "@/components/ui/switch";
import { useTheme, type ThemeMode } from "@/components/theme/theme-provider";
import {
  accents,
  fonts,
  type AccentOption,
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
  const { mode, setMode, font, setFont, accent, setAccent, density, setDensity } = useTheme();
  const [reducedMotion, setReducedMotion] = useState(false);

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
        <CardContent className="grid gap-3 px-6 pb-5 pt-1 sm:grid-cols-3 lg:grid-cols-6">
          {accents.map((a) => (
            <AccentCard
              key={a.id}
              option={a}
              active={accent === a.id}
              onSelect={() => setAccent(a.id)}
            />
          ))}
        </CardContent>
      </Card>

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
          : "border-[var(--color-border)] bg-[var(--color-surface-2)] hover:border-[var(--color-border-strong)]",
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
    <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.12em] text-[var(--color-primary)]">
      active
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
            : "border-[var(--color-border)] bg-[var(--color-surface-1)]",
        )}
        style={{ fontFamily: option.family }}
        aria-hidden
      >
        <span className="text-display text-2xl font-semibold tracking-tight">
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
