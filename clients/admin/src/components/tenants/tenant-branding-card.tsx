import { useEffect, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { Loader2, Palette, RotateCcw, Save } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import {
  ErrorBand,
  Field,
  FormSection,
  FormShell,
  LoadingRow,
} from "@/components/list";
import {
  DEFAULT_DARK_PALETTE,
  DEFAULT_LIGHT_PALETTE,
  getTenantTheme,
  resetTenantTheme,
  updateTenantTheme,
  type BrandAssetsDto,
  type PaletteDto,
  type TenantThemeDto,
} from "@/api/tenants";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";

/**
 * TenantBrandingCard — operator-facing theme editor for a single tenant.
 *
 * Scopes every API call to `tenantId` via the `tenant:` header override.
 * The endpoints are current-tenant-scoped server-side, so the operator
 * has to explicitly declare which tenant they're targeting — only root
 * operators get past the override middleware.
 *
 * Scope: palette (light + dark) + brand asset URLs. Typography and layout
 * fields exist on the server-side DTO but are intentionally omitted from
 * the v1 editor — they would bloat the page beyond the audit's scope and
 * are rarely tweaked in practice. Wire them up here if the need lands.
 */
export function TenantBrandingCard({ tenantId }: { tenantId: string }) {
  const queryClient = useQueryClient();

  const themeQueryKey = useMemo(
    () => ["tenant", tenantId, "theme"] as const,
    [tenantId],
  );

  const themeQuery = useQuery({
    queryKey: themeQueryKey,
    queryFn: () => getTenantTheme(tenantId),
    // Re-fetch when the page regains focus so other admins' edits are
    // picked up without a manual refresh.
    refetchOnWindowFocus: true,
  });

  const [draft, setDraft] = useState<TenantThemeDto | null>(null);

  // Seed draft state when the server payload arrives. We always replace
  // the draft on a fresh fetch so server-driven changes (other admin's
  // edits, reset action) are reflected in the editor.
  useEffect(() => {
    if (themeQuery.data) {
      setDraft(themeQuery.data);
    }
  }, [themeQuery.data]);

  const saveMutation = useMutation({
    mutationFn: (theme: TenantThemeDto) => updateTenantTheme(tenantId, theme),
    onSuccess: () => {
      toast.success("Branding saved");
      void queryClient.invalidateQueries({ queryKey: themeQueryKey });
    },
    onError: (err) =>
      toast.error("Save failed", { description: apiErr(err) }),
  });

  const resetMutation = useMutation({
    mutationFn: () => resetTenantTheme(tenantId),
    onSuccess: () => {
      toast.success("Branding reset to defaults");
      void queryClient.invalidateQueries({ queryKey: themeQueryKey });
    },
    onError: (err) =>
      toast.error("Reset failed", { description: apiErr(err) }),
  });

  if (themeQuery.isLoading) {
    return (
      <FormShell>
        <FormSection
          title="Branding"
          description="Operator-controlled colors + brand assets that drive this tenant's UI."
        >
          <LoadingRow label="Loading branding" />
        </FormSection>
      </FormShell>
    );
  }

  if (themeQuery.isError) {
    return (
      <FormShell>
        <FormSection title="Branding" description="">
          <ErrorBand message={apiErr(themeQuery.error)} />
        </FormSection>
      </FormShell>
    );
  }

  if (!draft) return null;

  const dirty =
    themeQuery.data && JSON.stringify(themeQuery.data) !== JSON.stringify(draft);

  const onLight = (next: Partial<PaletteDto>) =>
    setDraft((d) => (d ? { ...d, lightPalette: { ...d.lightPalette, ...next } } : d));
  const onDark = (next: Partial<PaletteDto>) =>
    setDraft((d) => (d ? { ...d, darkPalette: { ...d.darkPalette, ...next } } : d));
  const onAssets = (next: Partial<BrandAssetsDto>) =>
    setDraft((d) => (d ? { ...d, brandAssets: { ...d.brandAssets, ...next } } : d));

  return (
    <FormShell>
      <FormSection
        title="Branding"
        description={
          <span className="flex flex-wrap items-center gap-2">
            <Palette className="h-3.5 w-3.5 text-[var(--color-accent-signal)]" />
            <span>
              Theme tokens consumed by this tenant's apps (admin + dashboard)
              on sign-in. Live preview shows how the primary action would render.
            </span>
            {draft.isDefault && !dirty && (
              <Badge variant="outline" className="font-mono uppercase tracking-[0.14em]">
                default
              </Badge>
            )}
            {dirty && (
              <Badge variant="warning" className="font-mono uppercase tracking-[0.14em]">
                unsaved
              </Badge>
            )}
          </span>
        }
      >
        <div className="space-y-6">
          <ThemePreview palette={draft.lightPalette} label="Light preview" />

          <div className="grid gap-6 lg:grid-cols-2">
            <PaletteEditor
              title="Light palette"
              palette={draft.lightPalette}
              onChange={onLight}
              defaults={DEFAULT_LIGHT_PALETTE}
            />
            <PaletteEditor
              title="Dark palette"
              palette={draft.darkPalette}
              onChange={onDark}
              defaults={DEFAULT_DARK_PALETTE}
            />
          </div>

          <BrandAssetsEditor assets={draft.brandAssets} onChange={onAssets} />
        </div>
      </FormSection>

      <div className="flex flex-wrap items-center justify-end gap-2 border-t border-[var(--color-border)] px-6 py-3">
        <Button
          type="button"
          variant="ghost"
          onClick={() => resetMutation.mutate()}
          disabled={resetMutation.isPending || saveMutation.isPending}
          aria-label="Reset branding to defaults"
        >
          <RotateCcw className="mr-1.5 h-3.5 w-3.5" />
          {resetMutation.isPending ? "Resetting…" : "Reset to defaults"}
        </Button>
        <Button
          type="button"
          variant="signal"
          onClick={() => draft && saveMutation.mutate(draft)}
          disabled={!dirty || saveMutation.isPending}
        >
          {saveMutation.isPending ? (
            <Loader2 className="mr-1.5 h-3.5 w-3.5 animate-spin" />
          ) : (
            <Save className="mr-1.5 h-3.5 w-3.5" />
          )}
          {saveMutation.isPending ? "Saving…" : "Save branding"}
        </Button>
      </div>
    </FormShell>
  );
}

// ─────────────────────────────────────────────────────────────────────────
// Palette editor — color swatches paired with hex inputs
// ─────────────────────────────────────────────────────────────────────────

const PALETTE_FIELDS: ReadonlyArray<{ key: keyof PaletteDto; label: string }> = [
  { key: "primary", label: "Primary" },
  { key: "secondary", label: "Secondary" },
  { key: "tertiary", label: "Tertiary" },
  { key: "background", label: "Background" },
  { key: "surface", label: "Surface" },
  { key: "error", label: "Error" },
  { key: "warning", label: "Warning" },
  { key: "success", label: "Success" },
  { key: "info", label: "Info" },
];

function PaletteEditor({
  title,
  palette,
  onChange,
  defaults,
}: {
  title: string;
  palette: PaletteDto;
  onChange: (next: Partial<PaletteDto>) => void;
  defaults: PaletteDto;
}) {
  return (
    <div className="space-y-3 rounded-md border border-[var(--color-border)] bg-[var(--color-surface-2)] p-4">
      <div className="flex items-center justify-between">
        <h4 className="text-sm font-medium tracking-tight">{title}</h4>
        <button
          type="button"
          className="meta text-[var(--color-muted-foreground)] underline-offset-4 hover:text-[var(--color-foreground)] hover:underline"
          onClick={() => onChange(defaults)}
        >
          // reset this palette
        </button>
      </div>
      <div className="grid gap-2 sm:grid-cols-2">
        {PALETTE_FIELDS.map(({ key, label }) => (
          <ColorRow
            key={key}
            label={label}
            value={palette[key]}
            onChange={(v) => onChange({ [key]: v } as Partial<PaletteDto>)}
          />
        ))}
      </div>
    </div>
  );
}

function ColorRow({
  label,
  value,
  onChange,
}: {
  label: string;
  value: string;
  onChange: (next: string) => void;
}) {
  const valid = /^#[0-9a-f]{6}$/i.test(value);
  return (
    <div className="flex items-center gap-2">
      <label
        className="grid h-8 w-8 shrink-0 cursor-pointer place-items-center rounded-md ring-1 ring-inset ring-[var(--color-border)]"
        style={{ backgroundColor: valid ? value : undefined }}
        title={`Edit ${label}`}
      >
        <input
          type="color"
          value={valid ? value : "#000000"}
          onChange={(e) => onChange(e.target.value.toUpperCase())}
          className="sr-only"
          aria-label={`${label} color`}
        />
      </label>
      <div className="flex-1">
        <div className="font-mono text-[10px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
          {label}
        </div>
        <Input
          value={value}
          onChange={(e) => onChange(e.target.value.toUpperCase())}
          spellCheck={false}
          autoComplete="off"
          maxLength={9}
          className={cn(
            "h-7 px-2 font-mono text-[11.5px]",
            !valid && "border-[var(--color-destructive)]/60",
          )}
        />
      </div>
    </div>
  );
}

// ─────────────────────────────────────────────────────────────────────────
// Brand assets — URL editors for logo / logo-dark / favicon
// ─────────────────────────────────────────────────────────────────────────

function BrandAssetsEditor({
  assets,
  onChange,
}: {
  assets: BrandAssetsDto;
  onChange: (next: Partial<BrandAssetsDto>) => void;
}) {
  return (
    <div className="space-y-3 rounded-md border border-[var(--color-border)] bg-[var(--color-surface-2)] p-4">
      <h4 className="text-sm font-medium tracking-tight">Brand assets</h4>
      <p className="text-xs leading-relaxed text-[var(--color-muted-foreground)]">
        URLs to your hosted brand assets. For uploads, host the file via the
        Files module first and paste the resulting public URL here.
      </p>
      <div className="space-y-3">
        <AssetField
          id="logo-url"
          label="Logo URL"
          value={assets.logoUrl ?? ""}
          onChange={(v) =>
            onChange({ logoUrl: v || null, deleteLogo: v.length === 0 })
          }
        />
        <AssetField
          id="logo-dark-url"
          label="Logo URL (dark mode)"
          value={assets.logoDarkUrl ?? ""}
          onChange={(v) =>
            onChange({ logoDarkUrl: v || null, deleteLogoDark: v.length === 0 })
          }
        />
        <AssetField
          id="favicon-url"
          label="Favicon URL"
          value={assets.faviconUrl ?? ""}
          onChange={(v) =>
            onChange({ faviconUrl: v || null, deleteFavicon: v.length === 0 })
          }
        />
      </div>
    </div>
  );
}

function AssetField({
  id,
  label,
  value,
  onChange,
}: {
  id: string;
  label: string;
  value: string;
  onChange: (next: string) => void;
}) {
  return (
    <Field id={id} label={label}>
      <div className="flex items-center gap-2">
        <Input
          id={id}
          value={value}
          onChange={(e) => onChange(e.target.value)}
          placeholder="https://cdn.example.com/logo.svg"
          spellCheck={false}
          autoComplete="off"
          className="font-mono text-[12.5px]"
        />
        {value && (
          // Tiny inline preview thumbnail — reassures the operator the
          // URL points to an actually-loadable image. Failing loads just
          // hide via the onError handler below; no error UI necessary.
          <img
            src={value}
            alt=""
            onError={(e) => {
              (e.currentTarget as HTMLImageElement).style.display = "none";
            }}
            className="h-9 w-9 shrink-0 rounded-md object-contain ring-1 ring-inset ring-[var(--color-border)] bg-[var(--color-background)]"
          />
        )}
      </div>
    </Field>
  );
}

// ─────────────────────────────────────────────────────────────────────────
// Live preview swatch
// ─────────────────────────────────────────────────────────────────────────

function ThemePreview({ palette, label }: { palette: PaletteDto; label: string }) {
  return (
    <div
      className="rounded-md border border-[var(--color-border)] p-4"
      style={{ backgroundColor: palette.background, color: palette.secondary }}
    >
      <div className="meta mb-3 opacity-70">// {label}</div>
      <div
        className="rounded-md p-4"
        style={{ backgroundColor: palette.surface }}
      >
        <div className="mb-3 flex items-center justify-between gap-2">
          <span
            className="text-sm font-semibold"
            style={{ color: palette.secondary }}
          >
            Sample tenant page
          </span>
          <span
            className="rounded-full px-2 py-0.5 text-[10px] uppercase tracking-[0.14em]"
            style={{ backgroundColor: palette.success, color: palette.surface }}
          >
            active
          </span>
        </div>
        <p
          className="mb-3 text-[12.5px] leading-relaxed"
          style={{ color: palette.secondary, opacity: 0.75 }}
        >
          A short paragraph rendered with the chosen body color over the chosen
          surface, on the chosen page background. Action buttons use the primary token.
        </p>
        <div className="flex flex-wrap gap-2">
          <span
            className="rounded-md px-3 py-1.5 text-xs font-medium"
            style={{ backgroundColor: palette.primary, color: palette.surface }}
          >
            Primary action
          </span>
          <span
            className="rounded-md border px-3 py-1.5 text-xs font-medium"
            style={{
              borderColor: palette.primary,
              color: palette.primary,
              backgroundColor: "transparent",
            }}
          >
            Secondary
          </span>
          <span
            className="rounded-md px-2 py-1 text-[10.5px] font-mono uppercase tracking-[0.14em]"
            style={{ backgroundColor: palette.warning, color: palette.background }}
          >
            warn
          </span>
          <span
            className="rounded-md px-2 py-1 text-[10.5px] font-mono uppercase tracking-[0.14em]"
            style={{ backgroundColor: palette.error, color: palette.surface }}
          >
            error
          </span>
        </div>
      </div>
    </div>
  );
}

function apiErr(err: unknown): string {
  if (err instanceof ApiRequestError) {
    return err.problem?.detail ?? err.problem?.title ?? err.message;
  }
  if (err instanceof Error) return err.message;
  return "Unknown error";
}
