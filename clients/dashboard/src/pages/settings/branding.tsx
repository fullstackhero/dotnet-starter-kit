import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import type { TFunction } from "i18next";
import { toast } from "sonner";
import { Loader2, Palette, RotateCcw, Save } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { ErrorBand, Field } from "@/components/list";
import { SettingsSection } from "@/pages/settings/settings-layout";
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
 * BrandingSettings — tenant-facing theme editor for the *current* tenant.
 *
 * Mirrors the operator's TenantBrandingCard (clients/admin) but drops the
 * tenant targeting: the theme endpoints are current-tenant scoped, so a
 * tenant admin holding Tenants.UpdateTheme edits their own branding with no
 * `tenant:` header. The Branding tab only renders for holders of that
 * permission (see settings-layout TABS); a direct-URL visit without it still
 * mounts this page, and the API answers 403 — surfaced as an error band.
 *
 * Scope: palette (light + dark) + brand asset URLs. Typography and layout
 * fields exist on the server DTO but stay out of the v1 editor, matching the
 * admin card.
 */
const THEME_QUERY_KEY = ["tenant", "theme"] as const;

export function BrandingSettings() {
  const { t } = useTranslation("settings");
  const queryClient = useQueryClient();

  const themeQuery = useQuery({
    queryKey: THEME_QUERY_KEY,
    queryFn: getTenantTheme,
    // Do NOT refetch in the background: a focus/reconnect refetch produces a new
    // payload that the seed effect below would adopt, silently wiping the user's
    // unsaved edits mid-form. The data only changes here on initial load and on
    // our own save/reset invalidations, all of which SHOULD reseed the draft.
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
  });

  const [draft, setDraft] = useState<TenantThemeDto | null>(null);

  // Seed the draft from the server payload — fires on initial load and after our
  // own save/reset invalidations (background refetches are disabled above, so
  // this never clobbers in-progress edits).
  useEffect(() => {
    if (themeQuery.data) {
      setDraft(themeQuery.data);
    }
  }, [themeQuery.data]);

  const saveMutation = useMutation({
    mutationFn: (theme: TenantThemeDto) => updateTenantTheme(theme),
    onSuccess: () => {
      toast.success(t("branding.toastSaved"));
      void queryClient.invalidateQueries({ queryKey: THEME_QUERY_KEY });
    },
    onError: (err) => toast.error(t("branding.saveFailed"), { description: apiErr(err, t) }),
  });

  const resetMutation = useMutation({
    mutationFn: resetTenantTheme,
    onSuccess: () => {
      toast.success(t("branding.toastReset"));
      void queryClient.invalidateQueries({ queryKey: THEME_QUERY_KEY });
    },
    onError: (err) => toast.error(t("branding.resetFailed"), { description: apiErr(err, t) }),
  });

  if (themeQuery.isLoading) {
    return (
      <SettingsSection title={t("branding.title")} icon={Palette} description={t("branding.loadingDescription")}>
        <div className="flex items-center gap-2 text-[13px] text-[var(--color-muted-foreground)]">
          <Loader2 className="size-4 animate-spin" aria-hidden />
          <span>{t("branding.loading")}</span>
        </div>
      </SettingsSection>
    );
  }

  if (themeQuery.isError) {
    return (
      <SettingsSection title={t("branding.title")} icon={Palette}>
        <ErrorBand message={apiErr(themeQuery.error, t)} />
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
            {t("branding.badgeDefault")}
          </Badge>
        )}
        {dirty && (
          <Badge variant="warning" className="font-mono uppercase tracking-[0.14em]">
            {t("branding.badgeUnsaved")}
          </Badge>
        )}
      </div>
      <div className="flex items-center gap-2">
        <Button
          type="button"
          variant="ghost"
          onClick={() => resetMutation.mutate()}
          disabled={resetMutation.isPending || saveMutation.isPending}
          aria-label={t("branding.resetAriaLabel")}
        >
          <RotateCcw className="mr-1.5 h-3.5 w-3.5" />
          {resetMutation.isPending ? t("branding.resetting") : t("branding.resetToDefaults")}
        </Button>
        <Button
          type="button"
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
        <ThemePreview palette={draft.lightPalette} label={t("branding.lightPreview")} />

        <div className="grid gap-5 lg:grid-cols-2">
          <PaletteEditor
            title={t("branding.lightPalette")}
            palette={draft.lightPalette}
            onChange={onLight}
            defaults={DEFAULT_LIGHT_PALETTE}
          />
          <PaletteEditor
            title={t("branding.darkPalette")}
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

const PALETTE_FIELDS: ReadonlyArray<{ key: keyof PaletteDto; labelKey: string }> = [
  { key: "primary", labelKey: "branding.field.primary" },
  { key: "secondary", labelKey: "branding.field.secondary" },
  { key: "tertiary", labelKey: "branding.field.tertiary" },
  { key: "background", labelKey: "branding.field.background" },
  { key: "surface", labelKey: "branding.field.surface" },
  { key: "error", labelKey: "branding.field.error" },
  { key: "warning", labelKey: "branding.field.warning" },
  { key: "success", labelKey: "branding.field.success" },
  { key: "info", labelKey: "branding.field.info" },
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
  const { t } = useTranslation("settings");
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
          {t("branding.resetPalette")}
        </button>
      </div>
      <div className="grid gap-2 p-4 sm:grid-cols-2">
        {PALETTE_FIELDS.map(({ key, labelKey }) => (
          <ColorRow
            key={key}
            label={t(labelKey)}
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
  const { t } = useTranslation("settings");
  const valid = /^#[0-9a-f]{6}$/i.test(value);
  return (
    <div className="flex items-center gap-2.5">
      {/* Color chip — clicking opens the native color picker */}
      <label
        className="relative grid h-8 w-8 shrink-0 cursor-pointer place-items-center overflow-hidden rounded-lg shadow-sm ring-1 ring-inset ring-[var(--color-border)]"
        style={{ backgroundColor: valid ? value : undefined }}
        title={t("branding.pickColor", { label })}
      >
        <input
          type="color"
          value={valid ? value : "#000000"}
          onChange={(e) => onChange(e.target.value.toUpperCase())}
          className="sr-only"
          aria-label={t("branding.colorAriaLabel", { label })}
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
          aria-invalid={!valid}
          className={cn(
            "h-7 px-2 font-mono text-[11.5px]",
            !valid &&
              "border-[var(--color-destructive)]/60 focus-visible:ring-[var(--color-destructive)]/40",
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
  const { t } = useTranslation("settings");
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
          label={t("branding.assets.logo")}
          value={assets.logoUrl ?? ""}
          onChange={(v) =>
            onChange({ logoUrl: v || null, deleteLogo: v.length === 0 })
          }
        />
        <AssetField
          id="logo-dark-url"
          label={t("branding.assets.logoDark")}
          value={assets.logoDarkUrl ?? ""}
          onChange={(v) =>
            onChange({ logoDarkUrl: v || null, deleteLogoDark: v.length === 0 })
          }
        />
        <AssetField
          id="favicon-url"
          label={t("branding.assets.favicon")}
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
  const { t } = useTranslation("settings");
  return (
    <Field id={id} label={label}>
      <div className="flex items-center gap-2">
        <Input
          id={id}
          value={value}
          onChange={(e) => onChange(e.target.value)}
          placeholder={t("branding.assets.placeholder")}
          spellCheck={false}
          autoComplete="off"
          className="font-mono text-[12.5px]"
        />
        {value && (
          // Tiny inline preview thumbnail — reassures the tenant the URL
          // points to a loadable image. Failing loads just hide via onError.
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
  const { t } = useTranslation("settings");
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
        <div className="rounded-xl p-4" style={{ backgroundColor: palette.surface }}>
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
            {t("branding.preview.body")}
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

function apiErr(err: unknown, t: TFunction): string {
  if (err instanceof ApiRequestError) {
    return err.problem?.detail ?? err.problem?.title ?? err.message;
  }
  if (err instanceof Error) return err.message;
  return t("branding.unknownError");
}
