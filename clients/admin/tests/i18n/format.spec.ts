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
});
