import { createInstance } from "i18next";
import { expect, test } from "@playwright/test";
import { fallbackChain, missingKeyFallback } from "../../src/lib/i18n-fallback";

// A pt-BR translation is allowed to lag behind en-US, so a key pt-BR lacks must render the English
// string. The trap is a deployment whose default is pt-BR: a fallback chain of just the deployment
// default falls back from pt-BR to pt-BR, misses again, and the missing-key handler renders a
// capitalized key segment instead. Runs against the real i18next with the app's own handler.
const SUPPORTED = ["en-US", "pt-BR"] as const;

const instance = createInstance({
  lng: "pt-BR",
  fallbackLng: fallbackChain("pt-BR", SUPPORTED),
  supportedLngs: [...SUPPORTED],
  ns: ["identity"],
  defaultNS: "identity",
  resources: {
    "en-US": { identity: { title: "Users", greeting: "Hello" } },
    "pt-BR": { identity: { title: "Usuários" } },
  },
  interpolation: { escapeValue: false },
  parseMissingKeyHandler: missingKeyFallback,
});

test.beforeAll(async () => {
  await instance.init();
});

test.describe("fallback chain", () => {
  test("a key pt-BR lacks renders in English on a pt-BR deployment", () => {
    expect(instance.t("greeting")).toBe("Hello");
  });

  test("a translated key still renders in pt-BR", () => {
    expect(instance.t("title")).toBe("Usuários");
  });

  test("the chain always ends in en-US, without repeating it", () => {
    expect(fallbackChain("pt-BR", SUPPORTED)).toEqual(["pt-BR", "en-US"]);
    expect(fallbackChain("en-US", SUPPORTED)).toBe("en-US");
    expect(fallbackChain("fr-FR", SUPPORTED)).toBe("en-US");
  });
});
