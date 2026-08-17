import i18n from "i18next";
import LanguageDetector from "i18next-browser-languagedetector";
import { initReactI18next } from "react-i18next";
import enCommon from "@/locales/en-US/common.json";
import ptCommon from "@/locales/pt-BR/common.json";
import enNav from "@/locales/en-US/nav.json";
import ptNav from "@/locales/pt-BR/nav.json";
import enAuth from "@/locales/en-US/auth.json";
import ptAuth from "@/locales/pt-BR/auth.json";
import enSettings from "@/locales/en-US/settings.json";
import ptSettings from "@/locales/pt-BR/settings.json";
import enSessions from "@/locales/en-US/sessions.json";
import ptSessions from "@/locales/pt-BR/sessions.json";
import enUsers from "@/locales/en-US/users.json";
import ptUsers from "@/locales/pt-BR/users.json";
import enRoles from "@/locales/en-US/roles.json";
import ptRoles from "@/locales/pt-BR/roles.json";
import enImpersonation from "@/locales/en-US/impersonation.json";
import ptImpersonation from "@/locales/pt-BR/impersonation.json";
import enBilling from "@/locales/en-US/billing.json";
import ptBilling from "@/locales/pt-BR/billing.json";
import enTenants from "@/locales/en-US/tenants.json";
import ptTenants from "@/locales/pt-BR/tenants.json";
import enWebhooks from "@/locales/en-US/webhooks.json";
import ptWebhooks from "@/locales/pt-BR/webhooks.json";
import enAudits from "@/locales/en-US/audits.json";
import ptAudits from "@/locales/pt-BR/audits.json";
import enNotifications from "@/locales/en-US/notifications.json";
import ptNotifications from "@/locales/pt-BR/notifications.json";
import enHealth from "@/locales/en-US/health.json";
import ptHealth from "@/locales/pt-BR/health.json";
import enDashboard from "@/locales/en-US/dashboard.json";
import ptDashboard from "@/locales/pt-BR/dashboard.json";

// Canonical tags: specific (what the switcher offers, User.Locale persists, the claim carries).
export const SUPPORTED = ["en-US", "pt-BR"] as const;

// Translation namespaces keyed by name → per-locale catalog. This map is the
// single source string-migration waves extend: to add a namespace, import its
// two JSON catalogs and add one entry here — `resources` and `ns` below derive
// from it, so no other wiring changes.
const CATALOGS = {
  common: { "en-US": enCommon, "pt-BR": ptCommon },
  nav: { "en-US": enNav, "pt-BR": ptNav },
  auth: { "en-US": enAuth, "pt-BR": ptAuth },
  settings: { "en-US": enSettings, "pt-BR": ptSettings },
  sessions: { "en-US": enSessions, "pt-BR": ptSessions },
  users: { "en-US": enUsers, "pt-BR": ptUsers },
  roles: { "en-US": enRoles, "pt-BR": ptRoles },
  impersonation: { "en-US": enImpersonation, "pt-BR": ptImpersonation },
  billing: { "en-US": enBilling, "pt-BR": ptBilling },
  tenants: { "en-US": enTenants, "pt-BR": ptTenants },
  webhooks: { "en-US": enWebhooks, "pt-BR": ptWebhooks },
  audits: { "en-US": enAudits, "pt-BR": ptAudits },
  notifications: { "en-US": enNotifications, "pt-BR": ptNotifications },
  health: { "en-US": enHealth, "pt-BR": ptHealth },
  dashboard: { "en-US": enDashboard, "pt-BR": ptDashboard },
} as const;

// react-i18next wants resources shaped { <lng>: { <ns>: catalog } }. Build it from
// CATALOGS so SUPPORTED (what the switcher offers) and the namespace list stay the
// one source of truth for both the store and the type surface.
const resources = Object.fromEntries(
  SUPPORTED.map((lng) => [
    lng,
    Object.fromEntries(
      Object.entries(CATALOGS).map(([ns, byLng]) => [ns, byLng[lng]]),
    ),
  ]),
);

// i18next's nonExplicitSupportedLngs does NOT rewrite pt-PT->pt-BR. Normalize explicitly via
// convertDetectedLanguage: map any variant onto a canonical tag by its language part.
const CANON: Record<string, string> = { pt: "pt-BR", en: "en-US" };
const toCanonical = (lng: string) =>
  (SUPPORTED as readonly string[]).includes(lng) ? lng : (CANON[lng.split("-")[0]] ?? lng);

// Keep the document's language attribute in step with the active locale. index.html ships a
// static lang="en"; without this, a Portuguese UI still declares itself English to screen
// readers, browser translation and hyphenation. Registered once, before init, so it also fires
// for the initial language.
if (typeof document !== "undefined") {
  i18n.on("languageChanged", (lng) => {
    document.documentElement.lang = lng;
  });
}

// Called from main.tsx AFTER loadRuntimeConfig(), so fallbackLng reads the per-deployment
// default: the browser/persisted locale wins, the deployment default is only the fallback.
export function initI18n(deploymentDefault: string) {
  return i18n
    .use(LanguageDetector)
    .use(initReactI18next)
    .init({
      resources,
      ns: Object.keys(CATALOGS),
      fallbackLng: (SUPPORTED as readonly string[]).includes(deploymentDefault)
        ? deploymentDefault
        : "en-US",
      supportedLngs: [...SUPPORTED],
      defaultNS: "common",
      interpolation: { escapeValue: false },
      detection: {
        // NO cookie — localStorage only (the library default; key i18nextLng).
        order: ["querystring", "localStorage", "navigator"],
        caches: ["localStorage"],
        lookupQuerystring: "culture",
        convertDetectedLanguage: toCanonical, // pt/pt-PT->pt-BR, en/en-GB->en-US
      },
    });
}

export default i18n;
