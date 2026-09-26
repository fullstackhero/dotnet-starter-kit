import i18n from "@/i18n";
import { ApiRequestError } from "@/lib/api-client";

// Formatters read the active UI locale by default; callers may pin an explicit
// locale. Building an Intl formatter per row is wasteful in long ledgers and
// the locale only changes on a language switch, so cache one formatter per
// (locale) — the cache re-fills lazily under the new locale after a switch.
const activeLocale = (locale?: string) => locale ?? i18n.language ?? "en-US";

const dateLongByLocale = new Map<string, Intl.DateTimeFormat>();
function dateLongFor(locale: string) {
  let fmt = dateLongByLocale.get(locale);
  if (!fmt) {
    fmt = new Intl.DateTimeFormat(locale, {
      month: "short",
      day: "2-digit",
      year: "numeric",
    });
    dateLongByLocale.set(locale, fmt);
  }
  return fmt;
}

// "3:42 PM" — local wall-clock time. Intl renders in the browser's timezone.
const timeShortByLocale = new Map<string, Intl.DateTimeFormat>();
function timeShortFor(locale: string) {
  let fmt = timeShortByLocale.get(locale);
  if (!fmt) {
    fmt = new Intl.DateTimeFormat(locale, {
      hour: "numeric",
      minute: "2-digit",
    });
    timeShortByLocale.set(locale, fmt);
  }
  return fmt;
}

export function formatDate(iso: string | null | undefined, locale?: string) {
  if (!iso) return "—";
  return dateLongFor(activeLocale(locale)).format(new Date(iso));
}

// "APR 30 2026" — mono-caps tabular form for ledger/registry rows.
export function formatDateMono(iso: string | null | undefined, locale?: string) {
  if (!iso) return "—";
  return dateLongFor(activeLocale(locale)).format(new Date(iso)).toUpperCase().replace(",", "");
}

// "APR 30 2026 · 3:42 PM" — date + local time for audit/detail panels.
export function formatDateTimeMono(iso: string | null | undefined, locale?: string) {
  if (!iso) return "—";
  return `${formatDateMono(iso, locale)} · ${timeShortFor(activeLocale(locale)).format(new Date(iso))}`;
}

// Locale-grouped integer/decimal (e.g. en-US "1,234" · pt-BR "1.234").
export function formatNumber(value: number, locale?: string) {
  return new Intl.NumberFormat(activeLocale(locale)).format(value);
}

// "3d ago", "2mo ago" — terse relative time for the secondary line.
export function formatRelative(iso: string | null | undefined) {
  if (!iso) return "";
  const diffMs = Date.now() - new Date(iso).getTime();
  if (Number.isNaN(diffMs) || diffMs < 0) return "";
  const sec = Math.floor(diffMs / 1000);
  if (sec < 60) return i18n.t("relative.justNow");
  const min = Math.floor(sec / 60);
  if (min < 60) return i18n.t("relative.minutesAgo", { n: min });
  const hr = Math.floor(min / 60);
  if (hr < 24) return i18n.t("relative.hoursAgo", { n: hr });
  const day = Math.floor(hr / 24);
  if (day < 30) return i18n.t("relative.daysAgo", { n: day });
  const mo = Math.floor(day / 30);
  if (mo < 12) return i18n.t("relative.monthsAgo", { n: mo });
  const yr = Math.floor(day / 365);
  return i18n.t("relative.yearsAgo", { n: yr });
}

export function pad2(n: number) {
  return n.toString().padStart(2, "0");
}

// Mirror the server's slug derivation so editors can show a live preview.
export function slugify(value: string) {
  const lower = value.trim().toLowerCase();
  const chars = [...lower].map((c) => (/[a-z0-9]/.test(c) ? c : "-"));
  let s = chars.join("").replace(/^-+|-+$/g, "");
  while (s.includes("--")) s = s.replace(/--/g, "-");
  return s;
}

export function formatMoney(amount: number, currency: string, locale?: string) {
  try {
    return new Intl.NumberFormat(activeLocale(locale), {
      style: "currency",
      currency,
    }).format(amount);
  } catch {
    return `${amount.toFixed(2)} ${currency}`;
  }
}

// Flatten the ProblemDetails `errors` extension into a list of human messages.
// FluentValidation sends a field-keyed map ({ Email: ["..."] }); CustomException
// (e.g. Identity registration failures) sends a flat string[]. Both end up here.
function problemErrorMessages(errors: unknown): string[] {
  if (Array.isArray(errors)) {
    return errors.filter((e): e is string => typeof e === "string");
  }
  if (errors && typeof errors === "object") {
    return Object.values(errors as Record<string, unknown>)
      .flatMap((v) => (Array.isArray(v) ? v : [v]))
      .filter((e): e is string => typeof e === "string");
  }
  return [];
}

// Surface API/network/runtime errors with the same formatting everywhere.
// When the server includes specific reasons in the ProblemDetails `errors`
// extension (validation failures, Identity errors like "Email is already
// taken"), show those — they tell the user what to fix. Otherwise fall back to
// the Dev-only `reason` extension / detail / title so JwtBearer rejection
// causes (expired token, signing key drift, etc) stay visible in development.
export function describe(err: unknown): string {
  if (err instanceof ApiRequestError) {
    const details = problemErrorMessages(err.problem?.errors);
    if (details.length > 0) {
      return details.join(" ");
    }
    const reason =
      err.problem?.reason ??
      err.problem?.detail ??
      err.problem?.title ??
      err.message;
    return `${err.status} ${reason}`;
  }
  if (err instanceof Error) return err.message;
  return String(err);
}
