import i18n from "i18next";
import LanguageDetector from "i18next-browser-languagedetector";
import { initReactI18next } from "react-i18next";
import enCommon from "@/locales/en-US/common.json";
import ptCommon from "@/locales/pt-BR/common.json";
import enAuth from "@/locales/en-US/auth.json";
import ptAuth from "@/locales/pt-BR/auth.json";
import enSettings from "@/locales/en-US/settings.json";
import ptSettings from "@/locales/pt-BR/settings.json";
import enIdentity from "@/locales/en-US/identity.json";
import ptIdentity from "@/locales/pt-BR/identity.json";
import enOverview from "@/locales/en-US/overview.json";
import ptOverview from "@/locales/pt-BR/overview.json";
import enSubscription from "@/locales/en-US/subscription.json";
import ptSubscription from "@/locales/pt-BR/subscription.json";
import enActivity from "@/locales/en-US/activity.json";
import ptActivity from "@/locales/pt-BR/activity.json";
import enCatalog from "@/locales/en-US/catalog.json";
import ptCatalog from "@/locales/pt-BR/catalog.json";
import enTickets from "@/locales/en-US/tickets.json";
import ptTickets from "@/locales/pt-BR/tickets.json";
import enFiles from "@/locales/en-US/files.json";
import ptFiles from "@/locales/pt-BR/files.json";
import enAudits from "@/locales/en-US/audits.json";
import ptAudits from "@/locales/pt-BR/audits.json";
import enCommandPalette from "@/locales/en-US/commandPalette.json";
import ptCommandPalette from "@/locales/pt-BR/commandPalette.json";
import enNotifications from "@/locales/en-US/notifications.json";
import ptNotifications from "@/locales/pt-BR/notifications.json";
import enSystem from "@/locales/en-US/system.json";
import ptSystem from "@/locales/pt-BR/system.json";
import enChat from "@/locales/en-US/chat.json";
import ptChat from "@/locales/pt-BR/chat.json";
import enHealth from "@/locales/en-US/health.json";
import ptHealth from "@/locales/pt-BR/health.json";

// Canonical tags: specific (what the switcher offers, User.Locale persists, the claim carries).
export const SUPPORTED = ["en-US", "pt-BR"] as const;

type Catalog = Record<string, string>;

// Translation catalogs keyed by namespace, then by locale. To add a namespace
// in a later wave: import its two JSON files and add one row here — `resources`,
// the namespace list, and parity tests all derive from this single map, so no
// other wiring changes.
const catalogs: Record<string, Record<(typeof SUPPORTED)[number], Catalog>> = {
  common: { "en-US": enCommon, "pt-BR": ptCommon },
  auth: { "en-US": enAuth, "pt-BR": ptAuth },
  settings: { "en-US": enSettings, "pt-BR": ptSettings },
  identity: { "en-US": enIdentity, "pt-BR": ptIdentity },
  overview: { "en-US": enOverview, "pt-BR": ptOverview },
  subscription: { "en-US": enSubscription, "pt-BR": ptSubscription },
  activity: { "en-US": enActivity, "pt-BR": ptActivity },
  catalog: { "en-US": enCatalog, "pt-BR": ptCatalog },
  tickets: { "en-US": enTickets, "pt-BR": ptTickets },
  files: { "en-US": enFiles, "pt-BR": ptFiles },
  audits: { "en-US": enAudits, "pt-BR": ptAudits },
  commandPalette: { "en-US": enCommandPalette, "pt-BR": ptCommandPalette },
  notifications: { "en-US": enNotifications, "pt-BR": ptNotifications },
  system: { "en-US": enSystem, "pt-BR": ptSystem },
  chat: { "en-US": enChat, "pt-BR": ptChat },
  health: { "en-US": enHealth, "pt-BR": ptHealth },
};

export const NAMESPACES = Object.keys(catalogs);

// Pivot the namespace-first map into i18next's locale-first `resources` shape:
// { "en-US": { common: {…} }, "pt-BR": { common: {…} } }.
const resources = Object.fromEntries(
  SUPPORTED.map((lng) => [
    lng,
    Object.fromEntries(Object.entries(catalogs).map(([ns, byLng]) => [ns, byLng[lng]])),
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
      fallbackLng: (SUPPORTED as readonly string[]).includes(deploymentDefault)
        ? deploymentDefault
        : "en-US",
      supportedLngs: [...SUPPORTED],
      ns: NAMESPACES,
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
