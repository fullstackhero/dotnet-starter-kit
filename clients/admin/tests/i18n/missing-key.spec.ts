import { createInstance } from "i18next";
import { expect, test } from "@playwright/test";
import { missingKeyFallback } from "../../src/lib/i18n-fallback";

// i18next calls parseMissingKeyHandler for a missing key whether or not the call site passed a
// defaultValue, and the handler's return value is what renders. The first version of this handler
// only took the key, which quietly turned every `{ defaultValue }` in the app into a truncation of
// its own key — the permission matrix would have degraded to "Create" instead of "Create users".
// These run against the real i18next so the contract, not a re-implementation of it, is what holds.
const instance = createInstance({
  lng: "pt-BR",
  fallbackLng: "en-US",
  resources: {
    "pt-BR": { translation: { greeting: "Olá" } },
    "en-US": { translation: {} },
  },
  interpolation: { escapeValue: false },
  parseMissingKeyHandler: missingKeyFallback,
});

test.beforeAll(async () => {
  await instance.init();
});

test.describe("missing key fallback", () => {
  test("a caller's defaultValue survives the handler", () => {
    expect(
      instance.t("perm.entry.Permissions.Users.Create", { defaultValue: "Create users" }),
    ).toBe("Create users");
  });

  test("a key built from a server value degrades to its last segment", () => {
    // No defaultValue to fall back on: `status.${topup.status}` for a status the catalog predates.
    expect(instance.t("status.invoiced")).toBe("Invoiced");
    expect(instance.t("billing:status.refunded")).toBe("Refunded");
  });

  test("a translated key is untouched", () => {
    expect(instance.t("greeting")).toBe("Olá");
  });
});
