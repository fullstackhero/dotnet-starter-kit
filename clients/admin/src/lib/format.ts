import i18n from "@/i18n";

/**
 * Locale-aware formatting helpers built on the platform `Intl` APIs. The active
 * UI locale (`i18n.language`) is the default so formatted numbers, currency and
 * dates track the language switcher without every call site threading a locale.
 * Pass an explicit `locale` to override (e.g. rendering a fixed-locale export).
 */

// Active UI locale, guarded for the window before i18n has initialized.
const resolveLocale = (locale?: string): string => locale ?? i18n.language ?? "en-US";

export function formatNumber(value: number, locale?: string): string {
  return new Intl.NumberFormat(resolveLocale(locale)).format(value);
}

export function formatCurrency(amount: number, currency: string, locale?: string): string {
  try {
    return new Intl.NumberFormat(resolveLocale(locale), {
      style: "currency",
      currency,
    }).format(amount);
  } catch {
    // Intl throws RangeError on an unknown/malformed ISO 4217 code — degrade to a
    // readable amount + raw code rather than crashing the render.
    return `${amount.toFixed(2)} ${currency}`;
  }
}

export function formatDate(iso?: string | null, locale?: string): string {
  if (!iso) return "—";
  const date = new Date(iso);
  return Number.isNaN(date.getTime())
    ? iso
    : new Intl.DateTimeFormat(resolveLocale(locale), {
        month: "short",
        day: "2-digit",
        year: "numeric",
      }).format(date);
}
