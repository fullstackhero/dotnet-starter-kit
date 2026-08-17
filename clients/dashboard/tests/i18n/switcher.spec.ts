import { expect, test } from "@playwright/test";
import {
  IMPERSONATED_USER,
  OPERATOR_ACTOR,
  seedAuthedSession,
  seedImpersonationSession,
  TEST_USER,
} from "../helpers/auth-seed";
import { installShellMocks } from "../helpers/shell-mocks";

// Task 10 — the topbar language switcher. Switching to Português must:
//  (a) localize the UI in place (the "Language" section label becomes "Idioma"),
//  (b) PUT the chosen locale to /identity/profile with the name preserved
//      (a locale-only save must not wipe FirstName/LastName), and
//  (c) trigger a token refresh so the new `locale` JWT claim is minted.

/** Minimal decodable JWT for the refreshed session (auth-context decodes it). */
function fakeJwt(payload: Record<string, unknown>): string {
  const b64url = (obj: unknown) =>
    btoa(JSON.stringify(obj)).replace(/=+$/, "").replace(/\+/g, "-").replace(/\//g, "_");
  return [b64url({ alg: "HS256", typ: "JWT" }), b64url(payload), "sig"].join(".");
}

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, TEST_USER);
  await installShellMocks(page);
});

test.describe("language switcher", () => {
  test("switching to Português localizes the UI, persists the locale and refreshes the token", async ({
    page,
  }) => {
    // Accumulated rather than held in a `let`: TS narrows a nullable local that is
    // only assigned inside a callback down to `null` at the assertion site, which
    // makes every property read an error.
    const putBodies: Array<{ locale?: string; firstName?: string; lastName?: string }> = [];
    let refreshCalled = false;

    // GET returns the current (en-US) profile with a name so we can assert it is
    // preserved; PUT captures the body. Registered AFTER installShellMocks so
    // this handler wins (LIFO) over the default profile stub. updateMyProfile
    // itself issues a GET before the PUT, so both methods route through here.
    await page.route("**/api/v1/identity/profile", async (route) => {
      const method = route.request().method();
      if (method === "PUT") {
        putBodies.push(route.request().postDataJSON());
        await route.fulfill({ status: 200 });
        return;
      }
      if (method === "GET") {
        await route.fulfill({
          status: 200,
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            id: "u-test-1",
            firstName: "Alice",
            lastName: "Nguyen",
            phoneNumber: "",
            email: "alice@acme.com",
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
        name: "Alice Nguyen",
        tenant: "acme",
        locale: "pt-BR",
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

    await page.getByRole("menuitem", { name: "Português (BR)" }).click();

    // (b) the chosen locale was persisted, name preserved (no data loss). Poll
    // the captured body: the route handler that assigns it runs asynchronously.
    await expect.poll(() => putBodies[0]?.locale).toBe("pt-BR");
    expect(putBodies[0]?.firstName).toBe("Alice");
    expect(putBodies[0]?.lastName).toBe("Nguyen");

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
            firstName: "Alice",
            lastName: "Nguyen",
            phoneNumber: "",
            email: "alice@acme.com",
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
        name: "Alice Nguyen",
        tenant: "acme",
        locale: "pt-BR",
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
    expect(putBody.firstName).toBe("Alice");
    expect(putBody.lastName).toBe("Nguyen");
  });
});

// Language is the operator's own presentation choice: StartImpersonation strips
// the target's `locale` claim so the operator keeps reading in their language.
// /identity/profile is scoped to the impersonated subject, so persisting the
// switch would write the operator's language onto the target's profile, and
// hydrating from it would yank the operator into the target's language.
test.describe("language switcher during impersonation", () => {
  // Overrides the file-level authed session: the impersonation seed installs an
  // act_sub token and drops the refresh slot.
  test.beforeEach(async ({ page }) => {
    await seedImpersonationSession(page, IMPERSONATED_USER, OPERATOR_ACTOR);
  });

  test("switches the UI locally without persisting onto the impersonated user", async ({
    page,
  }) => {
    let putSeen = false;
    let refreshCalled = false;

    // The impersonated user's persisted locale is pt-BR — the operator's UI must
    // NOT hydrate from it.
    await page.route("**/api/v1/identity/profile", async (route) => {
      if (route.request().method() === "PUT") {
        putSeen = true;
        await route.fulfill({ status: 200 });
        return;
      }
      if (route.request().method() === "GET") {
        await route.fulfill({
          status: 200,
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            id: IMPERSONATED_USER.sub,
            firstName: IMPERSONATED_USER.firstName,
            lastName: IMPERSONATED_USER.lastName,
            phoneNumber: "",
            email: IMPERSONATED_USER.email,
            isActive: true,
            emailConfirmed: true,
            locale: "pt-BR",
          }),
        });
        return;
      }
      await route.fallback();
    });

    await page.route("**/api/v1/identity/token/refresh", async (route) => {
      refreshCalled = true;
      await route.fulfill({ status: 500 });
    });

    await page.goto("/");

    // The target's pt-BR did not leak into the operator's shell.
    await page.getByRole("button", { name: /open profile menu/i }).click();
    await expect(page.getByText("Language", { exact: true })).toBeVisible();

    await page.getByRole("menuitem", { name: "Português (BR)" }).click();

    // The switch still applies client-side…
    await expect(page.getByText("Idioma", { exact: true })).toBeVisible();
    // …but nothing was written to the impersonated user, and no token re-mint
    // fired (the locale claim belongs to the operator's own session).
    expect(putSeen).toBe(false);
    expect(refreshCalled).toBe(false);
  });
});
