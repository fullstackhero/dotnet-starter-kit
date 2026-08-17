import { expect, test } from "@playwright/test";
import { IMPERSONATED_USER, OPERATOR_ACTOR } from "../helpers/auth-seed";
import { installShellMocks } from "../helpers/shell-mocks";

// The cross-app impersonation handoff carries the OPERATOR's language.
//
// StartImpersonationCommandHandler strips the target's `locale` claim on purpose,
// so the API has nothing to negotiate from. The dashboard normally runs on a
// different origin than admin and therefore cannot read admin's persisted
// `i18nextLng`. Before the `locale` handoff parameter existed, the API culture
// fell through to the dashboard's own browser detection: the operator picked
// Português in admin, then read English error details here.
//
// The Accept-Language assertions are the real regression pin — apiFetch derives
// that header from i18n.language, so it is the observable consequence of the fix
// on the API side, not just a UI string swap.

/** An impersonation access token, as StartImpersonation would mint it: act_* claims, no target locale. */
function impersonationToken(): string {
  const b64url = (obj: unknown) =>
    btoa(JSON.stringify(obj)).replace(/=+$/, "").replace(/\+/g, "-").replace(/\//g, "_");
  const payload = {
    sub: IMPERSONATED_USER.sub,
    email: IMPERSONATED_USER.email,
    name: `${IMPERSONATED_USER.firstName} ${IMPERSONATED_USER.lastName}`,
    tenant: IMPERSONATED_USER.tenant,
    act_sub: OPERATOR_ACTOR.userId,
    act_tenant: OPERATOR_ACTOR.tenant,
    act_name: OPERATOR_ACTOR.name,
    permissions: [],
    exp: Math.floor(Date.now() / 1000) + 3600,
    iat: Math.floor(Date.now() / 1000),
  };
  return [b64url({ alg: "HS256", typ: "JWT" }), b64url(payload), "sig"].join(".");
}

function handoffUrl(locale?: string): string {
  const params = new URLSearchParams();
  params.set("token", impersonationToken());
  params.set("tenant", IMPERSONATED_USER.tenant);
  params.set("expiresAt", new Date(Date.now() + 3_600_000).toISOString());
  if (locale !== undefined) params.set("locale", locale);
  return `/#impersonate?${params.toString()}`;
}

/**
 * The SignalR client builds its own HTTP requests instead of going through
 * apiFetch, so `Accept-Language` on the hub negotiate is whatever the browser
 * sends, never the app's active locale. That is a general gap — it applies to
 * every session, not just impersonation — and is out of scope for this fix, so
 * it is named here rather than filtered away silently. Any OTHER path that stops
 * carrying the locale will fail the assertion below and force a decision.
 */
const NON_APIFETCH_PATHS = ["/api/v1/realtime/hub/negotiate"];

test.describe("impersonation handoff locale", () => {
  // Records Accept-Language per request path for every API call the shell makes.
  let observed: Array<{ path: string; header: string | null }>;

  const apiFetchLocales = () =>
    new Set(
      observed
        .filter((r) => !NON_APIFETCH_PATHS.includes(r.path))
        .map((r) => r.header ?? "<none>"),
    );

  test.beforeEach(async ({ page }) => {
    observed = [];
    await installShellMocks(page);
    // Registered AFTER the shell mocks so it runs FIRST (Playwright matches handlers
    // last-registered-first), then hands the request down via fallback().
    await page.route("**/api/v1/**", async (route) => {
      observed.push({
        path: new URL(route.request().url()).pathname,
        header: await route.request().headerValue("Accept-Language"),
      });
      await route.fallback();
    });
  });

  test("adopts the operator's locale for the UI and for Accept-Language", async ({ page }) => {
    await page.goto(handoffUrl("pt-BR"));

    // The shell rendered from the hash-installed token (no seeded session here) in
    // the operator's language: the button's own aria-label (common:profileMenu.open)
    // is already Portuguese, so the English name must not resolve at all.
    await expect(page.getByRole("button", { name: /open profile menu/i })).toHaveCount(0);
    await page.getByRole("button", { name: "Abrir menu do perfil" }).click();
    await expect(page.getByText("Idioma", { exact: true })).toBeVisible();

    // The API side of the same fix: the culture the backend negotiates from.
    await expect.poll(() => observed.length).toBeGreaterThan(0);
    expect(apiFetchLocales()).toEqual(new Set(["pt-BR"]));

    // The secret never survives in the URL bar.
    expect(page.url()).not.toContain("#impersonate");
    expect(page.url()).not.toContain("token=");
  });

  test("ignores an unsupported locale instead of forcing it", async ({ page }) => {
    await page.goto(handoffUrl("xx-YY"));

    await page.getByRole("button", { name: /open profile menu/i }).click();
    await expect(page.getByText("Language", { exact: true })).toBeVisible();

    await expect.poll(() => observed.length).toBeGreaterThan(0);
    expect(apiFetchLocales()).toEqual(new Set(["en-US"]));
  });

  test("still completes the handoff when no locale is carried", async ({ page }) => {
    await page.goto(handoffUrl());

    await page.getByRole("button", { name: /open profile menu/i }).click();
    await expect(page.getByText("Language", { exact: true })).toBeVisible();

    await expect.poll(() => observed.length).toBeGreaterThan(0);
    expect(apiFetchLocales()).toEqual(new Set(["en-US"]));
  });
});
