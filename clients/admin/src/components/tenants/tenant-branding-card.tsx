import { useEffect, useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { Loader2, Palette, RotateCcw, Save } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { ErrorBand, Field, LoadingRow, SettingsSection } from "@/components/list";
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
  const { t } = useTranslation("tenants");
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
      toast.success(t("branding.toast.saved"));
      void queryClient.invalidateQueries({ queryKey: themeQueryKey });
    },
    onError: (err) =>
      toast.error(t("branding.toast.saveFailed"), { description: apiErr(err, t("branding.unknownError")) }),
  });

  const resetMutation = useMutation({
    mutationFn: () => resetTenantTheme(tenantId),
    onSuccess: () => {
      toast.success(t("branding.toast.reset"));
      void queryClient.invalidateQueries({ queryKey: themeQueryKey });
    },
    onError: (err) =>
      toast.error(t("branding.toast.resetFailed"), { description: apiErr(err, t("branding.unknownError")) }),
  });

  if (themeQuery.isLoading) {
    return (
      <SettingsSection
        title={t("branding.title")}
        icon={Palette}
        description={t("branding.descriptionLoading")}
      >
        <LoadingRow label={t("branding.loading")} />
      </SettingsSection>
    );
  }

  if (themeQuery.isError) {
    return (
      <SettingsSection title={t("branding.title")} icon={Palette}>
        <ErrorBand message={apiErr(themeQuery.error, t("branding.unknownError"))} />
      </SettingsSection>
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

  const footer = (
    <div className="flex flex-wrap items-center justify-between gap-2">
      <div className="flex items-center gap-2">
        {draft.isDefault && !dirty && (
          <Badge variant="outline" className="font-mono uppercase tracking-[0.14em]">
            {t("branding.badge.default")}
          </Badge>
        )}
        {dirty && (
          <Badge variant="warning" className="font-mono uppercase tracking-[0.14em]">
            {t("branding.badge.unsaved")}
          </Badge>
        )}
      </div>
      <div className="flex items-center gap-2">
        <Button
          type="button"
          variant="ghost"
          onClick={() => resetMutation.mutate()}
          disabled={resetMutation.isPending || saveMutation.isPending}
          aria-label={t("branding.resetAria")}
        >
          <RotateCcw className="mr-1.5 h-3.5 w-3.5" />
          {resetMutation.isPending ? t("branding.resetting") : t("branding.reset")}
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
          {saveMutation.isPending ? t("branding.saving") : t("branding.save")}
        </Button>
      </div>
    </div>
  );

  return (
    <SettingsSection
      title={t("branding.title")}
      icon={Palette}
      description={t("branding.description")}
      footer={footer}
    >
      <div className="space-y-6">
        <ThemePreview palette={draft.lightPalette} label={t("branding.preview.light")} />

        <div className="grid gap-5 lg:grid-cols-2">
          <PaletteEditor
            title={t("branding.palette.light")}
            palette={draft.lightPalette}
            onChange={onLight}
            defaults={DEFAULT_LIGHT_PALETTE}
          />
          <PaletteEditor
            title={t("branding.palette.dark")}
            palette={draft.darkPalette}
            onChange={onDark}
            defaults={DEFAULT_DARK_PALETTE}
          />
        </div>

        <BrandAssetsEditor assets={draft.brandAssets} onChange={onAssets} />
      </div>
    </SettingsSection>
  );
}

// ─────────────────────────────────────────────────────────────────────────
// Palette editor — color swatches paired with hex inputs
// ─────────────────────────────────────────────────────────────────────────

const PALETTE_FIELDS: ReadonlyArray<{ key: keyof PaletteDto; label: string }> = [
  { key: "primary", label: "branding.paletteField.primary" },
  { key: "secondary", label: "branding.paletteField.secondary" },
  { key: "tertiary", label: "branding.paletteField.tertiary" },
  { key: "background", label: "branding.paletteField.background" },
  { key: "surface", label: "branding.paletteField.surface" },
  { key: "error", label: "branding.paletteField.error" },
  { key: "warning", label: "branding.paletteField.warning" },
  { key: "success", label: "branding.paletteField.success" },
  { key: "info", label: "branding.paletteField.info" },
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
  const { t } = useTranslation("tenants");
  return (
    <div className="overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-surface-2)]">
      <div className="flex items-center justify-between border-b border-[oklch(from_var(--color-border)_l_c_h_/_0.5)] px-4 py-2.5">
        <h4 className="text-[12.5px] font-semibold tracking-tight text-[var(--color-foreground)]">
          {title}
        </h4>
        <button
          type="button"
          className="inline-flex items-center gap-1 rounded-md px-2 py-1 text-[11px] font-medium text-[var(--color-muted-foreground)] transition-colors hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)]"
          onClick={() => onChange(defaults)}
        >
          <RotateCcw className="h-2.5 w-2.5" aria-hidden />
          {t("branding.palette.resetPalette")}
        </button>
      </div>
      <div className="grid gap-2 p-4 sm:grid-cols-2">
        {PALETTE_FIELDS.map(({ key, label }) => (
          <ColorRow
            key={key}
            label={t(label)}
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
  const { t } = useTranslation("tenants");
  const valid = /^#[0-9a-f]{6}$/i.test(value);
  return (
    <div className="flex items-center gap-2.5">
      {/* Color chip — clicking opens the native color picker */}
      <label
        className="relative grid h-8 w-8 shrink-0 cursor-pointer place-items-center overflow-hidden rounded-lg shadow-sm ring-1 ring-inset ring-[var(--color-border)]"
        style={{ backgroundColor: valid ? value : undefined }}
        title={t("branding.color.pick", { name: label })}
      >
        <input
          type="color"
          value={valid ? value : "#000000"}
          onChange={(e) => onChange(e.target.value.toUpperCase())}
          className="sr-only"
          aria-label={t("branding.color.aria", { name: label })}
        />
      </label>
      <div className="min-w-0 flex-1">
        <div className="mb-0.5 text-[10px] font-semibold uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
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
            !valid && "border-[var(--color-destructive)]/60 focus-visible:ring-[var(--color-destructive)]/40",
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
  const { t } = useTranslation("tenants");
  return (
    <div className="overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-surface-2)]">
      <div className="border-b border-[oklch(from_var(--color-border)_l_c_h_/_0.5)] px-4 py-2.5">
        <h4 className="text-[12.5px] font-semibold tracking-tight text-[var(--color-foreground)]">
          {t("branding.assets.title")}
        </h4>
        <p className="mt-0.5 text-[11.5px] leading-relaxed text-[var(--color-muted-foreground)]">
          {t("branding.assets.description")}
        </p>
      </div>
      <div className="space-y-4 p-4">
        <AssetField
          id="logo-url"
          label={t("branding.assets.logoUrl")}
          value={assets.logoUrl ?? ""}
          onChange={(v) =>
            onChange({ logoUrl: v || null, deleteLogo: v.length === 0 })
          }
        />
        <AssetField
          id="logo-dark-url"
          label={t("branding.assets.logoDarkUrl")}
          value={assets.logoDarkUrl ?? ""}
          onChange={(v) =>
            onChange({ logoDarkUrl: v || null, deleteLogoDark: v.length === 0 })
          }
        />
        <AssetField
          id="favicon-url"
          label={t("branding.assets.faviconUrl")}
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
            className="h-9 w-9 shrink-0 rounded-lg object-contain ring-1 ring-inset ring-[var(--color-border)] bg-[var(--color-background)]"
          />
        )}
      </div>
    </Field>
  );
}

// ─────────────────────────────────────────────────────────────────────────
// Live preview — shows how primary-action buttons + surface tokens render
// ─────────────────────────────────────────────────────────────────────────

function ThemePreview({ palette, label }: { palette: PaletteDto; label: string }) {
  const { t } = useTranslation("tenants");
  return (
    <div
      className="overflow-hidden rounded-xl border border-[var(--color-border)]"
      style={{ backgroundColor: palette.background }}
    >
      {/* Preview header bar */}
      <div
        className="flex items-center justify-between border-b px-4 py-2"
        style={{
          borderColor: `${palette.surface}55`,
          backgroundColor: palette.surface,
        }}
      >
        <span
          className="text-[11px] font-semibold uppercase tracking-[0.12em] opacity-60"
          style={{ color: palette.secondary }}
        >
          {label}
        </span>
        <span
          className="rounded-full px-2 py-0.5 text-[10px] font-medium uppercase tracking-[0.1em]"
          style={{ backgroundColor: palette.success, color: palette.surface }}
        >
          {t("branding.preview.live")}
        </span>
      </div>

      {/* Preview body */}
      <div className="p-4">
        <div
          className="rounded-xl p-4"
          style={{ backgroundColor: palette.surface }}
        >
          <div className="mb-3 flex items-center justify-between gap-2">
            <span
              className="text-[13px] font-semibold"
              style={{ color: palette.secondary }}
            >
              {t("branding.preview.samplePage")}
            </span>
            <span
              className="rounded-full px-2 py-0.5 text-[10px] font-medium uppercase tracking-[0.1em]"
              style={{ backgroundColor: palette.success, color: palette.surface }}
            >
              {t("branding.preview.active")}
            </span>
          </div>
          <p
            className="mb-4 text-[12.5px] leading-relaxed"
            style={{ color: palette.secondary, opacity: 0.72 }}
          >
            {t("branding.preview.paragraph")}
          </p>
          <div className="flex flex-wrap gap-2">
            {/* Primary action */}
            <span
              className="inline-flex items-center rounded-lg px-3 py-1.5 text-xs font-medium shadow-sm"
              style={{ backgroundColor: palette.primary, color: palette.surface }}
            >
              {t("branding.preview.primaryAction")}
            </span>
            {/* Outline secondary */}
            <span
              className="inline-flex items-center rounded-lg border px-3 py-1.5 text-xs font-medium"
              style={{
                borderColor: palette.primary,
                color: palette.primary,
                backgroundColor: "transparent",
              }}
            >
              {t("branding.preview.secondary")}
            </span>
            {/* Warning pill */}
            <span
              className="inline-flex items-center rounded-lg px-2.5 py-1 text-[10.5px] font-mono font-medium uppercase tracking-[0.1em]"
              style={{ backgroundColor: palette.warning, color: palette.background }}
            >
              {t("branding.preview.warn")}
            </span>
            {/* Error pill */}
            <span
              className="inline-flex items-center rounded-lg px-2.5 py-1 text-[10.5px] font-mono font-medium uppercase tracking-[0.1em]"
              style={{ backgroundColor: palette.error, color: palette.surface }}
            >
              {t("branding.preview.error")}
            </span>
          </div>
        </div>
      </div>
    </div>
  );
}

function apiErr(err: unknown, fallback: string): string {
  if (err instanceof ApiRequestError) {
    return err.problem?.detail ?? err.problem?.title ?? err.message;
  }
  if (err instanceof Error) return err.message;
  return fallback;
}
