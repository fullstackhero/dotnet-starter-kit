import { ApiRequestError } from "@/lib/api-client";

const dateLong = new Intl.DateTimeFormat("en-US", {
  month: "short",
  day: "2-digit",
  year: "numeric",
});

export function formatDate(iso: string | null | undefined) {
  if (!iso) return "—";
  return dateLong.format(new Date(iso));
}

// "APR 30 2026" — mono-caps tabular form for ledger/registry rows.
export function formatDateMono(iso: string | null | undefined) {
  if (!iso) return "—";
  return dateLong.format(new Date(iso)).toUpperCase().replace(",", "");
}

// "3:42 PM" — local wall-clock time. Intl renders in the browser's timezone.
const timeShort = new Intl.DateTimeFormat("en-US", {
  hour: "numeric",
  minute: "2-digit",
});

// "APR 30 2026 · 3:42 PM" — date + local time for audit/detail panels.
export function formatDateTimeMono(iso: string | null | undefined) {
  if (!iso) return "—";
  return `${formatDateMono(iso)} · ${timeShort.format(new Date(iso))}`;
}

// "3d ago", "2mo ago" — terse relative time for the secondary line.
export function formatRelative(iso: string | null | undefined) {
  if (!iso) return "";
  const diffMs = Date.now() - new Date(iso).getTime();
  if (Number.isNaN(diffMs) || diffMs < 0) return "";
  const sec = Math.floor(diffMs / 1000);
  if (sec < 60) return "just now";
  const min = Math.floor(sec / 60);
  if (min < 60) return `${min}m ago`;
  const hr = Math.floor(min / 60);
  if (hr < 24) return `${hr}h ago`;
  const day = Math.floor(hr / 24);
  if (day < 30) return `${day}d ago`;
  const mo = Math.floor(day / 30);
  if (mo < 12) return `${mo}mo ago`;
  const yr = Math.floor(day / 365);
  return `${yr}y ago`;
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

export function formatMoney(amount: number, currency: string) {
  try {
    return new Intl.NumberFormat(undefined, {
      style: "currency",
      currency,
    }).format(amount);
  } catch {
    return `${amount.toFixed(2)} ${currency}`;
  }
}

// Surface API/network/runtime errors with the same formatting everywhere.
// Prefers the Dev-only `reason` extension on ProblemDetails so JwtBearer
// rejection causes (expired token, signing key drift, etc) are visible
// in toast descriptions during development.
export function describe(err: unknown): string {
  if (err instanceof ApiRequestError) {
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
