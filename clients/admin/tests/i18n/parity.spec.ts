import { expect, test } from "@playwright/test";
import { readdirSync, readFileSync } from "node:fs";
import path from "node:path";

// en-US is the source catalog; pt-BR is a translation that may lag behind it. A key (or a whole
// namespace file) pt-BR lacks is fine: it renders in English at runtime (fallback-chain.spec.ts).
// What breaks the UI is a pt-BR key nothing renders (an orphan, usually a renamed or deleted
// en-US key) and a translation whose placeholders drift from the English, so those two fail here.
// Read via fs (cwd = the admin app dir) to avoid ESM JSON import-attribute rules.
const readCatalog = (locale: string, ns: string): Record<string, string> =>
  JSON.parse(readFileSync(path.resolve("src/locales", locale, `${ns}.json`), "utf8"));

// A contributor who adds a namespace without a translation ships no pt-BR file at all.
const readTranslation = (ns: string): Record<string, string> => {
  try {
    return readCatalog("pt-BR", ns);
  } catch (error) {
    if ((error as NodeJS.ErrnoException).code === "ENOENT") return {};
    throw error;
  }
};

const catalogFiles = (locale: string): string[] =>
  readdirSync(path.resolve("src/locales", locale))
    .filter((f) => f.endsWith(".json"))
    .map((f) => f.replace(/\.json$/, ""))
    .sort();

const namespaces = ["common", "nav", "auth", "settings", "sessions", "users", "roles", "impersonation", "billing", "tenants", "webhooks", "audits", "notifications", "health", "dashboard"];

test.describe("catalog parity", () => {
  // The list above is hand-maintained, so it can drift from what actually ships: a namespace
  // file added without an entry here escapes every parity assertion below. This is the
  // completeness check that makes the hand-maintained list safe to keep.
  test("the namespace list covers every catalog file on disk", () => {
    expect(catalogFiles("en-US")).toEqual([...namespaces].sort());
  });

  test("every pt-BR catalog file has an en-US counterpart", () => {
    expect(catalogFiles("pt-BR").filter((ns) => !namespaces.includes(ns))).toEqual([]);
  });

  for (const ns of namespaces) {
    test(`${ns}: pt-BR has no key en-US lacks`, () => {
      const en = readCatalog("en-US", ns);
      const orphans = Object.keys(readTranslation(ns)).filter((key) => !(key in en));
      expect(orphans).toEqual([]);
    });
  }

  // A translation that drops or renames an interpolation renders the placeholder as literal text
  // ("Olá, {{name}}") or silently loses the value, and neither shows up as a missing key. i18next
  // resolves `{{name}}` and `{{count, number}}` alike, so the variable name is taken up to the
  // first comma and the formatter ignored.
  const placeholders = (value: string): string[] =>
    [...value.matchAll(/{{\s*([^},]+?)\s*(?:,[^}]*)?}}/g)].map((m) => m[1]).sort();

  for (const ns of namespaces) {
    test(`${ns}: en-US and pt-BR interpolate the same variables`, () => {
      const en = readCatalog("en-US", ns);
      const pt = readTranslation(ns);
      const divergent = Object.keys(en)
        .filter((key) => typeof en[key] === "string" && typeof pt[key] === "string")
        .map((key) => ({ key, en: placeholders(en[key]), pt: placeholders(pt[key]) }))
        .filter(({ en: a, pt: b }) => a.join("|") !== b.join("|"));

      expect(divergent).toEqual([]);
    });
  }
});
