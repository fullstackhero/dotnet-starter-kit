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
});
