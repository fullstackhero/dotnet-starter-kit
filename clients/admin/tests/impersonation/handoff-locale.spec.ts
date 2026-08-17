import { expect, test } from "@playwright/test";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installAdminShellMocks, ADMIN_PERMS } from "../helpers/shell-mocks";
import { mockJsonResponse } from "../helpers/api-mocks";

// The PRODUCER half of the cross-app impersonation handoff.
//
// The dashboard side is pinned by clients/dashboard/tests/impersonation/handoff-locale.spec.ts,
// but nothing asserted that this app actually PUTS the operator's locale in the URL —
// dropping `params.set("locale", …)` left every suite green. The dashboard cannot recover
// the operator's language on its own: the server strips the target's `locale` claim, and
// the two apps normally sit on different origins so `i18nextLng` is not shared.
//
// window.open is stubbed rather than allowed to open a tab: the handoff URL is the thing
// under test, and the real dashboard origin is not served in this suite.

/** An Active grant whose actor IS the seeded operator — required for Re-open to render. */
const OWN_ACTIVE_GRANT = {
  id: "g-active-1",
  jti: "jti-active-1",
  actorUserId: TEST_USER.sub,
  actorUserName: "rootadmin",
  actorTenantId: "root",
  impersonatedUserId: "u-target",
  impersonatedUserName: "alice@acme.com",
  impersonatedTenantId: "acme",
  reason: "Investigating a support ticket",
  startedAtUtc: "2026-05-23T10:00:00Z",
  expiresAtUtc: "2026-05-23T11:00:00Z",
  status: "Active",
};

declare global {
  interface Window {
    __openedUrls?: string[];
  }
}

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, { ...TEST_USER, permissions: [...ADMIN_PERMS] });
  await installAdminShellMocks(page);
  await mockJsonResponse(page, "**/api/v1/identity/impersonation/grants*", [OWN_ACTIVE_GRANT]);

  await page.route("**/api/v1/identity/impersonation/start", async (route) => {
    await route.fulfill({
      status: 200,
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        accessToken: "header.payload.sig",
        accessTokenExpiresAt: "2026-05-23T11:00:00Z",
      }),
    });
  });

  await page.addInitScript(() => {
    window.__openedUrls = [];
    window.open = (url?: string | URL) => {
      window.__openedUrls!.push(String(url ?? ""));
      return null;
    };
  });
});

/**
 * Drives Re-open → reason → start. Re-open pre-fills the user so the picker step is
 * skipped, which keeps this focused on the handoff instead of the search flow.
 */
async function startImpersonationViaReopen(
  page: import("@playwright/test").Page,
  labels: { reopen: string; reason: string; start: string },
) {
  const main = page.getByRole("main");
  await expect(main.getByText("alice@acme.com", { exact: true })).toBeVisible({ timeout: 10_000 });

  await main.getByRole("button", { name: labels.reopen }).first().click();

  const dialog = page.getByRole("dialog");
  await dialog.getByLabel(labels.reason).fill("Customer ticket 4821");
  await dialog.getByRole("button", { name: labels.start }).click();
}

function handoffParams(url: string): URLSearchParams {
  const marker = "#impersonate?";
  const at = url.indexOf(marker);
  expect(at, `handoff URL is not an #impersonate hash handoff: ${url}`).toBeGreaterThan(-1);
  return new URLSearchParams(url.slice(at + marker.length));
}

test.describe("impersonation handoff carries the operator locale", () => {
  test("sends the operator's selected language, not the deployment default", async ({ page }) => {
    // `?culture=` is first in the detection order (lookupQuerystring: "culture"), so this
    // boots the app in Portuguese without touching localStorage.
    await page.goto("/impersonation?culture=pt-BR");

    await startImpersonationViaReopen(page, {
      reopen: "Reabrir",
      reason: "Motivo",
      start: "Iniciar personificação de 15 min",
    });

    await expect.poll(() => page.evaluate(() => window.__openedUrls?.length ?? 0)).toBe(1);
    const opened = (await page.evaluate(() => window.__openedUrls![0]))!;
    const params = handoffParams(opened);

    expect(params.get("locale")).toBe("pt-BR");
    // The pre-existing contract must survive the added parameter.
    expect(params.get("token")).toBe("header.payload.sig");
    expect(params.get("tenant")).toBe("acme");
    expect(params.get("expiresAt")).toBe("2026-05-23T11:00:00Z");
  });

  test("sends en-US when the operator is reading in English", async ({ page }) => {
    await page.goto("/impersonation?culture=en-US");

    await startImpersonationViaReopen(page, {
      reopen: "Re-open",
      reason: "Reason",
      start: "Start 15-min impersonation",
    });

    await expect.poll(() => page.evaluate(() => window.__openedUrls?.length ?? 0)).toBe(1);
    const opened = (await page.evaluate(() => window.__openedUrls![0]))!;
    expect(handoffParams(opened).get("locale")).toBe("en-US");
  });
});
