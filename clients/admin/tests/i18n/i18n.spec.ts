import { expect, test } from "@playwright/test";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installAdminShellMocks, ADMIN_PERMS, paged } from "../helpers/shell-mocks";
import { mockJsonResponse } from "../helpers/api-mocks";

// These specs cover Task 8 (i18n bootstrap) + Task 9 (Accept-Language header).
// The dashboard route ("/") renders the AppShell + Topbar; the Topbar's profile
// menu button is a stable "the app mounted" signal. main.tsx awaits initI18n()
// before mounting React, so a boot failure there would leave nothing to find.

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, { ...TEST_USER, permissions: [...ADMIN_PERMS] });
  await installAdminShellMocks(page);

  // Dashboard load endpoints (page content); topbar renders regardless, but
  // mocking these keeps the route quiet and deterministic.
  await mockJsonResponse(page, "**/api/v1/tenants**", paged([], { totalCount: 0 }));
  await mockJsonResponse(page, "**/api/v1/billing/plans**", []);
  await mockJsonResponse(page, "**/api/v1/billing/invoices**", paged([], { totalCount: 0 }));
});

test.describe("i18n", () => {
  test("app boots with i18n initialized", async ({ page }) => {
    await page.goto("/");

    // The Topbar (which consumes the i18n instance for the profile-locale sync)
    // rendered → initI18n() resolved and React mounted.
    await expect(
      page.getByRole("button", { name: /open profile menu/i }),
    ).toBeVisible({ timeout: 10_000 });
  });

  test("apiFetch sends Accept-Language matching the active locale", async ({ page }) => {
    let seenLang: string | null = null;

    // Registered AFTER installAdminShellMocks so this handler wins (LIFO) and
    // can inspect the request header. Return a profile whose locale matches the
    // default active locale so the sync effect does not switch languages.
    await page.route("**/api/v1/identity/profile", async (route) => {
      if (route.request().method() !== "GET") {
        await route.fallback();
        return;
      }
      seenLang = route.request().headers()["accept-language"] ?? null;
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          id: "u-test-1",
          isActive: true,
          emailConfirmed: true,
          locale: "en-US",
        }),
      });
    });

    const profileReq = page.waitForRequest(
      (r) => r.url().includes("/api/v1/identity/profile") && r.method() === "GET",
      { timeout: 10_000 },
    );
    await page.goto("/");
    await profileReq;

    expect(seenLang).toBe("en-US");
  });

  test("apiFetch sends Accept-Language: pt-BR once the locale is Portuguese", async ({ page }) => {
    // Boot the app in Portuguese via the ?culture querystring — the i18n detector gives
    // querystring top priority (order: ["querystring", ...]), so the active locale is pt-BR
    // before the first apiFetch runs. A hardcoded "en-US" in apiFetch would fail this.
    let seenLang: string | null = null;

    // Return a profile whose locale already matches pt-BR so the topbar's sync effect does not
    // switch the language back.
    await page.route("**/api/v1/identity/profile", async (route) => {
      if (route.request().method() !== "GET") {
        await route.fallback();
        return;
      }
      seenLang = route.request().headers()["accept-language"] ?? null;
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          id: "u-test-1",
          isActive: true,
          emailConfirmed: true,
          locale: "pt-BR",
        }),
      });
    });

    const profileReq = page.waitForRequest(
      (r) => r.url().includes("/api/v1/identity/profile") && r.method() === "GET",
      { timeout: 10_000 },
    );
    await page.goto("/?culture=pt-BR");
    await profileReq;

    expect(seenLang).toBe("pt-BR");
  });
});
