import { readdirSync, readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { expect, test } from "@playwright/test";

// Every namespace must carry identical key sets across locales. A key present
// in one catalog but missing in the other silently falls back to the default
// language at runtime, shipping a mixed-locale UI. This guards each catalog as
// new namespaces land (add one assertion per namespace here per wave).
const load = (locale: string, ns: string): Record<string, string> =>
  JSON.parse(
    readFileSync(
      fileURLToPath(new URL(`../../src/locales/${locale}/${ns}.json`, import.meta.url)),
      "utf8",
    ),
  );

// One entry per shipped namespace. Add new namespaces here as each wave lands.
const NAMESPACES = ["common", "auth", "settings", "identity", "overview", "subscription", "activity", "catalog", "tickets", "files", "audits", "commandPalette", "notifications", "system", "chat", "health"];

test.describe("i18n catalog parity", () => {
  // The list above is hand-maintained, so it can drift from what actually ships: a namespace
  // file added without an entry here escapes every parity assertion below and can go out
  // half-translated with the suite green. This is the completeness check that makes the
  // hand-maintained list safe to keep.
  test("the namespace list covers every catalog file on disk", () => {
    const dir = fileURLToPath(new URL("../../src/locales/en-US", import.meta.url));
    const onDisk = readdirSync(dir)
      .filter((f) => f.endsWith(".json"))
      .map((f) => f.replace(/\.json$/, ""))
      .sort();

    expect(onDisk).toEqual([...NAMESPACES].sort());
  });

  for (const ns of NAMESPACES) {
    test(`${ns}: en-US and pt-BR expose the same keys`, () => {
      const en = load("en-US", ns);
      const pt = load("pt-BR", ns);
      expect(Object.keys(en).sort()).toEqual(Object.keys(pt).sort());
    });
  }

  // Matching key sets are not enough: a translation that drops or renames an
  // interpolation renders the placeholder as literal text ("Olá, {{name}}") or
  // silently loses the value, and neither shows up as a missing key. i18next
  // resolves `{{name}}` and `{{count, number}}` alike, so the variable name is
  // taken up to the first comma and the formatter is ignored.
  const placeholders = (value: string): string[] =>
    [...value.matchAll(/{{\s*([^},]+?)\s*(?:,[^}]*)?}}/g)].map((m) => m[1]).sort();

  for (const ns of NAMESPACES) {
    test(`${ns}: en-US and pt-BR interpolate the same variables`, () => {
      const en = load("en-US", ns);
      const pt = load("pt-BR", ns);
      const divergent = Object.keys(en)
        .filter((key) => typeof en[key] === "string" && typeof pt[key] === "string")
        .map((key) => ({
          key,
          en: placeholders(en[key]),
          pt: placeholders(pt[key]),
        }))
        .filter(({ en: a, pt: b }) => a.join("|") !== b.join("|"));

      expect(divergent).toEqual([]);
    });
  }
});
