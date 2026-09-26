import { expect, test } from "@playwright/test";
import { readdirSync, readFileSync } from "node:fs";
import path from "node:path";

// A missing/extra key in one locale silently falls back to the key or the other
// locale at runtime. Assert both catalogs expose the identical key set per
// namespace so a half-translated string is caught at test time, not in the UI.
// Read via fs (cwd = the admin app dir) to avoid ESM JSON import-attribute rules.
const readCatalog = (locale: string, ns: string): Record<string, string> =>
  JSON.parse(readFileSync(path.resolve("src/locales", locale, `${ns}.json`), "utf8"));

const namespaces = ["common", "nav", "auth", "settings", "sessions", "users", "roles", "impersonation", "billing", "tenants", "webhooks", "audits", "notifications", "health", "dashboard"];

test.describe("catalog parity", () => {
  // The list above is hand-maintained, so it can drift from what actually ships: a namespace
  // file added without an entry here escapes every parity assertion below and can go out
  // half-translated with the suite green. This is the completeness check that makes the
  // hand-maintained list safe to keep.
  test("the namespace list covers every catalog file on disk", () => {
    const onDisk = readdirSync(path.resolve("src/locales", "en-US"))
      .filter((f) => f.endsWith(".json"))
      .map((f) => f.replace(/\.json$/, ""))
      .sort();

    expect(onDisk).toEqual([...namespaces].sort());
  });

  for (const ns of namespaces) {
    test(`${ns}: en-US and pt-BR expose the same keys`, () => {
      const en = Object.keys(readCatalog("en-US", ns)).sort();
      const pt = Object.keys(readCatalog("pt-BR", ns)).sort();
      expect(en).toEqual(pt);
    });
  }

  // Matching key sets are not enough: a translation that drops or renames an interpolation
  // renders the placeholder as literal text ("Olá, {{name}}") or silently loses the value, and
  // neither shows up as a missing key. i18next resolves `{{name}}` and `{{count, number}}` alike,
  // so the variable name is taken up to the first comma and the formatter ignored.
  const placeholders = (value: string): string[] =>
    [...value.matchAll(/{{\s*([^},]+?)\s*(?:,[^}]*)?}}/g)].map((m) => m[1]).sort();

  for (const ns of namespaces) {
    test(`${ns}: en-US and pt-BR interpolate the same variables`, () => {
      const en = readCatalog("en-US", ns);
      const pt = readCatalog("pt-BR", ns);
      const divergent = Object.keys(en)
        .filter((key) => typeof en[key] === "string" && typeof pt[key] === "string")
        .map((key) => ({ key, en: placeholders(en[key]), pt: placeholders(pt[key]) }))
        .filter(({ en: a, pt: b }) => a.join("|") !== b.join("|"));

      expect(divergent).toEqual([]);
    });
  }
});
