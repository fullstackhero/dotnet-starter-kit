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
});
