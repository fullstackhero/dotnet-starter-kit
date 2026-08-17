import { expect, test } from "@playwright/test";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installAdminShellMocks, ADMIN_PERMS, paged } from "../helpers/shell-mocks";
import { mockJsonResponse } from "../helpers/api-mocks";

// Task 10 — the topbar language switcher. Switching to Português must:
//  (a) localize the UI in place (the "Language" section label becomes "Idioma"),
//  (b) PUT the chosen locale to /identity/profile, and
//  (c) trigger a token refresh so the new `locale` JWT claim is minted.

/** Minimal decodable JWT for the refreshed session (auth-context decodes it). */
function fakeJwt(payload: Record<string, unknown>): string {
  const b64url = (obj: unknown) =>
    btoa(JSON.stringify(obj)).replace(/=+$/, "").replace(/\+/g, "-").replace(/\//g, "_");
  return [b64url({ alg: "HS256", typ: "JWT" }), b64url(payload), "sig"].join(".");
}

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, { ...TEST_USER, permissions: [...ADMIN_PERMS] });
  await installAdminShellMocks(page);

  await mockJsonResponse(page, "**/api/v1/tenants**", paged([], { totalCount: 0 }));
  await mockJsonResponse(page, "**/api/v1/billing/plans**", []);
  await mockJsonResponse(page, "**/api/v1/billing/invoices**", paged([], { totalCount: 0 }));
});

test.describe("language switcher", () => {
  test("switching to Português localizes the UI, persists the locale and refreshes the token", async ({
    page,
  }) => {
    let refreshCalled = false;

    // GET returns the current (en-US) profile with a name so we can assert it is
    // preserved; the PUT body is read off the resolved request below (race-free);
    // both methods share this one route (LIFO wins).
    await page.route("**/api/v1/identity/profile", async (route) => {
      const method = route.request().method();
      if (method === "PUT") {
        await route.fulfill({ status: 200 });
        return;
      }
      if (method === "GET") {
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
        return;
      }
      await route.fallback();
    });

    // The onSuccess token refresh — capture the call and return a fresh session
    // carrying the new locale claim so the auth context stays valid.
    await page.route("**/api/v1/identity/token/refresh", async (route) => {
      refreshCalled = true;
      const token = fakeJwt({
        sub: "u-test-1",
        email: TEST_USER.email,
        name: "Root Admin",
        tenant: "root",
        locale: "pt-BR",
        permissions: [...ADMIN_PERMS],
        exp: Math.floor(Date.now() / 1000) + 3600,
        iat: Math.floor(Date.now() / 1000),
      });
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ token, refreshToken: "fresh-refresh-token" }),
      });
    });

    await page.goto("/");

    // Open the profile dropdown, then the (default en-US) language section.
    await page.getByRole("button", { name: /open profile menu/i }).click();
    await expect(page.getByText("Language", { exact: true })).toBeVisible();

    const putRequest = page.waitForRequest(
      (r) => r.url().includes("/api/v1/identity/profile") && r.method() === "PUT",
    );
    await page.getByRole("menuitem", { name: "Português (BR)" }).click();
    // Read the body straight off the resolved request — waitForRequest fires on
    // dispatch, before the route handler could capture it into a shared variable.
    const putBody = (await putRequest).postDataJSON() as {
      locale?: string;
      firstName?: string;
      lastName?: string;
    };

    // (b) the chosen locale was persisted, name preserved (no data loss).
    expect(putBody.locale).toBe("pt-BR");
    expect(putBody.firstName).toBe("Root");
    expect(putBody.lastName).toBe("Admin");

    // (a) the section label localized in place (menu kept open on select).
    await expect(page.getByText("Idioma", { exact: true })).toBeVisible();

    // (c) the token refresh fired to re-mint the locale claim.
    await expect.poll(() => refreshCalled).toBe(true);
  });

  // Regression (data-loss): the PUT body must be built from a fresh server read
  // inside updateMyProfile, NOT from the topbar's ["identity","profile"] query
  // snapshot. If that query is still pending (or failed) when the user switches
  // language, the old code sent firstName/lastName = undefined and the backend
  // wiped the name. We gate every GET so the profile is provably NOT loaded in
  // the component at click time, then release it and assert the PUT still
  // carries the name.
  test("preserves firstName/lastName even when the profile query has not loaded", async ({
    page,
  }) => {
    // A gate held closed until we've already clicked the language item, so at
    // click time no GET has resolved — profile.data in the topbar is undefined.
    let releaseGet: () => void = () => {};
    const getGate = new Promise<void>((resolve) => {
      releaseGet = resolve;
    });

    await page.route("**/api/v1/identity/profile", async (route) => {
      const method = route.request().method();
      if (method === "PUT") {
        await route.fulfill({ status: 200 });
        return;
      }
      if (method === "GET") {
        await getGate;
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
        return;
      }
      await route.fallback();
    });

    await page.route("**/api/v1/identity/token/refresh", async (route) => {
      const token = fakeJwt({
        sub: "u-test-1",
        email: TEST_USER.email,
        name: "Root Admin",
        tenant: "root",
        locale: "pt-BR",
        permissions: [...ADMIN_PERMS],
        exp: Math.floor(Date.now() / 1000) + 3600,
        iat: Math.floor(Date.now() / 1000),
      });
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ token, refreshToken: "fresh-refresh-token" }),
      });
    });

    await page.goto("/");

    // The dropdown and language list render from i18n/SUPPORTED, not the profile,
    // so the menu is usable while the (gated) profile GET is still pending.
    await page.getByRole("button", { name: /open profile menu/i }).click();
    await expect(page.getByText("Language", { exact: true })).toBeVisible();

    const putRequest = page.waitForRequest(
      (r) => r.url().includes("/api/v1/identity/profile") && r.method() === "PUT",
    );
    await page.getByRole("menuitem", { name: "Português (BR)" }).click();

    // Only now let the profile reads resolve: updateMyProfile's own GET feeds the PUT.
    releaseGet();
    // Read the body straight off the resolved request (race-free — waitForRequest
    // fires on dispatch, before the route handler would have captured anything).
    const putBody = (await putRequest).postDataJSON() as {
      locale?: string;
      firstName?: string;
      lastName?: string;
    };

    expect(putBody.locale).toBe("pt-BR");
    expect(putBody.firstName).toBe("Root");
    expect(putBody.lastName).toBe("Admin");
  });
});
