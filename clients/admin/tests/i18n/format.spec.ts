import { expect, test } from "@playwright/test";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installAdminShellMocks, ADMIN_PERMS, paged } from "../helpers/shell-mocks";
import { mockJsonResponse } from "../helpers/api-mocks";

// Task 11 — locale-aware Intl formatters (src/lib/format.ts). Exercised through the
// real Vite module graph (dynamic import over the dev server) so the `@/i18n`
// alias and i18n wiring match production. Explicit-locale calls assert
// locale-correct grouping/currency; null/invalid inputs assert the fallbacks.

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, { ...TEST_USER, permissions: [...ADMIN_PERMS] });
  await installAdminShellMocks(page);
  await mockJsonResponse(page, "**/api/v1/tenants**", paged([], { totalCount: 0 }));
  await mockJsonResponse(page, "**/api/v1/billing/plans**", []);
  await mockJsonResponse(page, "**/api/v1/billing/invoices**", paged([], { totalCount: 0 }));
});

test.describe("format.ts", () => {
  test("formatters render per explicit locale and fall back safely", async ({ page }) => {
    await page.goto("/");
    await expect(
      page.getByRole("button", { name: /open profile menu/i }),
    ).toBeVisible({ timeout: 10_000 });

    const out = await page.evaluate(async () => {
      // Resolved by the Vite dev server in the browser, not by the bundler or by
      // tsc — a served URL, not a module specifier. Kept in a variable so the
      // typechecker doesn't try (and fail) to resolve it from disk.
      const formatModuleUrl = "/src/lib/format.ts";
      const m = await import(/* @vite-ignore */ formatModuleUrl);
      return {
        curPt: m.formatCurrency(1234.5, "BRL", "pt-BR"),
        curEn: m.formatCurrency(1234.5, "BRL", "en-US"),
        numPt: m.formatNumber(1234567.89, "pt-BR"),
        numEn: m.formatNumber(1234567.89, "en-US"),
        datePt: m.formatDate("2026-01-15T12:00:00Z", "pt-BR"),
        dateEn: m.formatDate("2026-01-15T12:00:00Z", "en-US"),
        dateNull: m.formatDate(null),
        dateBad: m.formatDate("not-a-date"),
        curBad: m.formatCurrency(10, "NOTACUR", "en-US"),
      };
    });

    // Currency: BRL symbol + locale-specific grouping/decimal separators.
    expect(out.curPt).toContain("R$");
    expect(out.curPt).toContain("1.234,50");
    expect(out.curEn).toContain("1,234.50");

    // Number grouping differs by locale.
    expect(out.numPt).toBe("1.234.567,89");
    expect(out.numEn).toBe("1,234,567.89");

    // Date renders per locale and carries the year; the two locales differ.
    expect(out.datePt).toContain("2026");
    expect(out.dateEn).toContain("2026");
    expect(out.datePt).not.toBe(out.dateEn);

    // Documented fallbacks: nullish -> em dash, unparseable -> echo, bad currency -> amount + code.
    expect(out.dateNull).toBe("—");
    expect(out.dateBad).toBe("not-a-date");
    expect(out.curBad).toBe("10.00 NOTACUR");
  });

  // Every assertion above passes an explicit locale, so `resolveLocale` — the branch every
  // production call site actually takes — was never exercised: replacing it with `() => "en-US"`
  // kept the spec green while pt-BR users saw 1,234,567.89.
  test("formatters follow the active locale with no locale argument", async ({ page }) => {
    await page.goto("/?culture=pt-BR");
    // The profile-menu label is itself translated, so wait on something language-neutral.
    await expect(page.locator("html")).toHaveAttribute("lang", "pt-BR");

    const out = await page.evaluate(async () => {
      const formatModuleUrl = "/src/lib/format.ts";
      const m = await import(/* @vite-ignore */ formatModuleUrl);
      return {
        num: m.formatNumber(1234567.89),
        date: m.formatDate("2026-01-15T12:00:00Z"),
      };
    });

    expect(out.num).toBe("1.234.567,89");
    expect(out.date).toContain("2026");
    expect(out.date).not.toMatch(/Jan\b/);
  });

  test("a list count is grouped and pluralized by the locale, not by English rules", async ({ page }) => {
    // Two separate defects met in this one chip. The count reached i18next as a raw number, so a
    // Portuguese UI read "1234" beside currency and dates that were correctly grouped. And the
    // header pluralized by appending "s" to the unit, which is an English rule: "organização" + "s"
    // is not a word. Both are the catalog's job now, so both are asserted on the rendered chip.
    await mockJsonResponse(page, "**/api/v1/tenants**", paged([], { totalCount: 1234, pageSize: 20 }));

    await page.goto("/tenants?culture=pt-BR");
    await expect(page.locator("html")).toHaveAttribute("lang", "pt-BR");

    await expect(page.getByText("1.234 organizações", { exact: true })).toBeVisible();
    await expect(page.getByText("1.234 organizações registradas nesta instância.")).toBeVisible();
  });

  // `resolveLocale` reads `i18n.language` at call time, so a date formatted on a previous render
  // keeps the old locale until something re-renders the component. Nothing in format.ts subscribes
  // to `languageChanged`; what makes the switch reach a mounted list is that every component that
  // formats also calls useTranslation, and that subscription is easy to drop in a refactor with no
  // test noticing. Driving the real switcher against an already-rendered date is what notices.
  test("reformats a date already on screen when the language changes under it", async ({ page }) => {
    let invoiceReads = 0;
    await page.route("**/api/v1/billing/invoices?*", async (route) => {
      if (route.request().method() !== "GET") {
        await route.fallback();
        return;
      }
      invoiceReads += 1;
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(
          paged([
            {
              id: "inv-1",
              tenantId: "acme",
              invoiceNumber: "INV-2026-0001",
              periodYear: 2026,
              periodMonth: 5,
              currency: "USD",
              subtotalAmount: 129.5,
              status: "Draft",
              // Midday UTC so the rendered day is May 1 whatever timezone the runner sits in.
              createdAtUtc: "2026-05-01T12:00:00Z",
              issuedAtUtc: null,
              dueAtUtc: null,
              paidAtUtc: null,
              voidedAtUtc: null,
              notes: null,
              lineItems: [],
            },
          ]),
        ),
      });
    });

    await page.route("**/api/v1/identity/profile", async (route) => {
      if (route.request().method() === "PUT") {
        await route.fulfill({ status: 200 });
        return;
      }
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          id: "u-test-1",
          firstName: "Root",
          lastName: "Admin",
          phoneNumber: "",
          isActive: true,
          emailConfirmed: true,
          locale: "en-US",
        }),
      });
    });
    // The token re-mint is covered by switcher.spec.ts and is not what this asserts; it still has
    // to succeed, because the failure path clears the session and takes the list off screen.
    await page.route("**/api/v1/identity/token/refresh", async (route) => {
      const b64url = (obj: unknown) =>
        btoa(JSON.stringify(obj)).replace(/=+$/, "").replace(/\+/g, "-").replace(/\//g, "_");
      const now = Math.floor(Date.now() / 1000);
      const token = [
        b64url({ alg: "HS256", typ: "JWT" }),
        b64url({
          sub: "u-test-1",
          email: TEST_USER.email,
          name: "Root Admin",
          tenant: "root",
          locale: "pt-BR",
          permissions: [...ADMIN_PERMS],
          exp: now + 3600,
          iat: now,
        }),
        "sig",
      ].join(".");
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ token, refreshToken: "fresh-refresh-token" }),
      });
    });

    await page.goto("/billing/invoices");

    const main = page.getByRole("main");
    await expect(main.getByText("INV-2026-0001", { exact: true })).toBeVisible({ timeout: 10_000 });
    // "May 01, 2026" under en-US. The invoice date is the only date this list renders.
    await expect(main).toContainText("May");

    await page.getByRole("button", { name: /open profile menu/i }).click();
    await page.getByRole("menuitem", { name: "Português (BR)" }).click();
    await expect(page.getByText("Idioma", { exact: true })).toBeVisible();

    // "01 de mai. de 2026". Not a remount: the route never changed and the list was read once.
    await expect(main).toContainText("mai.");
    await expect(main).not.toContainText("May");
    expect(invoiceReads).toBe(1);
  });
});
