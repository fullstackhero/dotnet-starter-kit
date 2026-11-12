import i18n from "i18next";
import LanguageDetector from "i18next-browser-languagedetector";
import { initReactI18next } from "react-i18next";
import { fallbackChain, missingKeyFallback } from "@/lib/i18n-fallback";
import enCommon from "@/locales/en-US/common.json";
import enNav from "@/locales/en-US/nav.json";
import enAuth from "@/locales/en-US/auth.json";
import enNotifications from "@/locales/en-US/notifications.json";
import enDashboard from "@/locales/en-US/dashboard.json";

// Canonical tags: specific (what the switcher offers, User.Locale persists, the claim carries).
export const SUPPORTED = ["en-US", "pt-BR"] as const;

type Catalog = Record<string, string>;

// en-US catalogs the app shell renders with (layout, sign-in, notification bell, the landing
// dashboard). They ship in the main bundle so the first paint never waits on a catalog.
const EAGER_CATALOGS = {
  common: enCommon,
  nav: enNav,
  auth: enAuth,
  notifications: enNotifications,
  dashboard: enDashboard,
} satisfies Record<string, Catalog>;

// en-US catalogs only route pages use. Each is its own small chunk, loaded by loadNamespaces()
// in parallel with the page's chunk (see `lazyNamed` in src/routes.tsx), so the page never
// renders before its text is there.
//
// To add a namespace: add its en-US JSON here (or to EAGER_CATALOGS when the shell uses it) and
// list it on the routes that use it. `resources`, `ns` and the t() key types all derive from
// these two maps. Its pt-BR file (optional) is picked up by src/locales/pt-BR.ts; a namespace or
// key pt-BR lacks renders in English through fallbackChain.
const LAZY_CATALOGS = {
  settings: () => import("@/locales/en-US/settings.json"),
  sessions: () => import("@/locales/en-US/sessions.json"),
  users: () => import("@/locales/en-US/users.json"),
  roles: () => import("@/locales/en-US/roles.json"),
  impersonation: () => import("@/locales/en-US/impersonation.json"),
  billing: () => import("@/locales/en-US/billing.json"),
  tenants: () => import("@/locales/en-US/tenants.json"),
  webhooks: () => import("@/locales/en-US/webhooks.json"),
  audits: () => import("@/locales/en-US/audits.json"),
  health: () => import("@/locales/en-US/health.json"),
} satisfies Record<string, () => Promise<{ default: Catalog }>>;

export type LazyNamespace = keyof typeof LAZY_CATALOGS;

// What src/i18next.d.ts checks every t() key against: the eager and lazy catalogs together.
export type Catalogs = typeof EAGER_CATALOGS & {
  [K in LazyNamespace]: Awaited<ReturnType<(typeof LAZY_CATALOGS)[K]>>["default"];
};

const NAMESPACES = [...Object.keys(EAGER_CATALOGS), ...Object.keys(LAZY_CATALOGS)];

const pendingNamespaces = new Map<LazyNamespace, Promise<void>>();

// Loads route namespaces into en-US, the terminal fallback. Other languages need no work here:
// loadLanguage adds every namespace of a language in one go. A failed load is forgotten so the
// next navigation retries it.
export function loadNamespaces(namespaces: readonly LazyNamespace[]): Promise<void> {
  return Promise.all(
    namespaces.map((ns) => {
      let pending = pendingNamespaces.get(ns);
      if (!pending) {
        pending = LAZY_CATALOGS[ns]().then(
          ({ default: catalog }) => {
            i18n.addResourceBundle("en-US", ns, catalog, true, true);
          },
          (error: unknown) => {
            pendingNamespaces.delete(ns);
            throw error;
          },
        );
        pendingNamespaces.set(ns, pending);
      }
      return pending;
    }),
  ).then(() => undefined);
}

// en-US is the terminal fallback: its shell catalogs ship in the main bundle and its route
// catalogs load with their routes. Every other language is one lazy chunk, fetched before the UI
// switches to it.
const LAZY_LANGUAGES: Partial<
  Record<(typeof SUPPORTED)[number], () => Promise<{ default: Record<string, Catalog> }>>
> = {
  "pt-BR": () => import("@/locales/pt-BR"),
};
const loadedLanguages = new Set<string>(["en-US"]);

async function loadLanguage(lng: string) {
  const load = LAZY_LANGUAGES[lng as (typeof SUPPORTED)[number]];
  if (!load || loadedLanguages.has(lng)) return;
  const { default: catalogs } = await load();
  for (const ns of NAMESPACES) {
    const catalog = catalogs[ns];
    if (catalog) i18n.addResourceBundle(lng, ns, catalog, true, true);
  }
  loadedLanguages.add(lng);
}

let latestSwitch = 0;

// Every language switch goes through here so the catalog lands before the UI re-renders in it:
// react-i18next re-renders on languageChanged, not when a bundle is added. When the chunk fails
// to load this rejects before i18next sees the new language, so neither the UI nor the
// detector's persisted choice moves. Resolves false when a later switch landed while this
// catalog was loading: applying it now would undo the newer choice, and reporting its failure
// would contradict the language the user now sees.
export async function changeLanguage(lng: string): Promise<boolean> {
  const thisSwitch = ++latestSwitch;
  try {
    await loadLanguage(lng);
  } catch (error) {
    if (thisSwitch !== latestSwitch) return false;
    throw error;
  }
  if (thisSwitch !== latestSwitch) return false;
  await i18n.changeLanguage(lng);
  return true;
}

// i18next's nonExplicitSupportedLngs does NOT rewrite pt-PT->pt-BR. Normalize explicitly via
// convertDetectedLanguage: map any variant onto a canonical tag by its language part.
const CANON: Record<string, string> = { pt: "pt-BR", en: "en-US" };
const toCanonical = (lng: string) =>
  (SUPPORTED as readonly string[]).includes(lng) ? lng : (CANON[lng.split("-")[0]] ?? lng);

// Keep the document's language attribute in step with the active locale. index.html ships a
// static lang="en"; without this, a Portuguese UI still declares itself English to screen
// readers, browser translation and hyphenation. Registered once, before init, so it also fires
// for the initial language. It reads resolvedLanguage, the language the text is actually in:
// when a catalog failed to load, `lng` is still the requested one while the UI shows English.
if (typeof document !== "undefined") {
  i18n.on("languageChanged", (lng) => {
    document.documentElement.lang = i18n.resolvedLanguage ?? lng;
  });
}

// Called from main.tsx AFTER loadRuntimeConfig(), so fallbackLng reads the per-deployment
// default: the browser/persisted locale wins, the deployment default is only the fallback.
export async function initI18n(deploymentDefault: string) {
  await i18n
    .use(LanguageDetector)
    .use(initReactI18next)
    .init({
      // Route catalogs are added later through loadNamespaces().
      resources: { "en-US": EAGER_CATALOGS },
      ns: NAMESPACES,
      fallbackLng: fallbackChain(deploymentDefault, SUPPORTED),
      supportedLngs: [...SUPPORTED],
      defaultNS: "common",
      interpolation: { escapeValue: false },
      // A key the catalog has not caught up with must not render as itself ("status.invoiced") or
      // swallow the caller's English fallback. Both rules live in missingKeyFallback, which the
      // suite exercises directly; this only adds the development warning.
      parseMissingKeyHandler: (key: string, defaultValue?: string | null) => {
        if (import.meta.env.DEV) {
          console.warn(`[i18n] missing key: ${key}`);
        }
        return missingKeyFallback(key, defaultValue);
      },
      detection: {
        // NO cookie — localStorage only, under this app's own key. The detector's default is the
        // bare "i18nextLng", which both apps would claim on a shared origin and which collides with
        // any other i18next app deployed beside them; every other persisted value here is already
        // namespaced the same way (fsh.<app>.*).
        order: ["querystring", "localStorage", "navigator"],
        caches: ["localStorage"],
        lookupLocalStorage: "fsh.admin.lng",
        lookupQuerystring: "culture",
        convertDetectedLanguage: toCanonical, // pt/pt-PT->pt-BR, en/en-GB->en-US
      },
    });

  // The detected (or deployment-default) language may be a lazy one: fetch it before the first
  // render so a pt-BR user never sees English first. If the chunk fails, the app still boots and
  // renders English through the fallback chain. i18next has already set (and the detector saved)
  // `language` to the requested tag; switching to en-US here would overwrite that saved choice,
  // so code that needs the language on screen reads resolvedLanguage, which stays en-US.
  try {
    await changeLanguage(i18n.language);
  } catch (error) {
    console.warn(`[i18n] could not load the ${i18n.language} catalog; showing English.`, error);
  }
}

export default i18n;
