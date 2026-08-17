// E2E coverage for the deactivated-tenant terminal flow.
//
// Once a tenant is switched off, the guard (MultitenancyModule) rejects every
// request with a 403 carrying the `Multitenancy.TenantDeactivated` code. A
// global query/mutation error hook (query-client.ts → isTenantDeactivatedError)
// routes to the dedicated /tenant-deactivated page instead of leaving a dead
// error banner under a half-loaded dashboard.
//
// The detector keys off the ProblemDetails `code`, never the `detail` prose:
// `detail` is localized under the request's Accept-Language, so a pt-BR reader
// would otherwise stay stuck on failing screens.

import { expect, test } from "@playwright/test";
import { mockProblemDetails } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks } from "../helpers/shell-mocks";

const TENANT_DEACTIVATED_CODE = "Multitenancy.TenantDeactivated";

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, TEST_USER);
  await installShellMocks(page);
});

test.describe("tenant deactivated mid-session", () => {
  test("a coded 403 routes to the terminal page", async ({ page }) => {
    await mockProblemDetails(page, "**/api/v1/catalog/products**", 403, {
      title: "Forbidden",
      detail: "This tenant has been deactivated. Contact your administrator.",
      code: TENANT_DEACTIVATED_CODE,
    });

    await page.goto("/catalog/products");

    await expect(page).toHaveURL(/\/tenant-deactivated$/);
    await expect(page.getByRole("heading", { name: /tenant deactivated/i })).toBeVisible();
  });

  // The regression this guards: the detector used to match the English detail
  // text. Under pt-BR the server localizes that prose, so a text match fails and
  // the user is left on a dead screen. The code is culture-independent.
  test("routes on a localized 403 — detection does not depend on the detail prose", async ({
    page,
  }) => {
    await mockProblemDetails(page, "**/api/v1/catalog/products**", 403, {
      title: "Proibido",
      detail: "Esta organização foi desativada. Entre em contato com o administrador.",
      code: TENANT_DEACTIVATED_CODE,
    });

    await page.goto("/?culture=pt-BR");
    await page.goto("/catalog/products?culture=pt-BR");

    await expect(page).toHaveURL(/\/tenant-deactivated/);
    await expect(page.getByRole("heading", { name: /organização desativada/i })).toBeVisible();
  });

  // An unrelated 403 (a permission denial) must NOT hijack the app into the
  // terminal page — only the deactivated-tenant code does.
  test("an unrelated 403 does not route to the terminal page", async ({ page }) => {
    await mockProblemDetails(page, "**/api/v1/catalog/products**", 403, {
      title: "Forbidden",
      detail: "Unauthorized access.",
      code: "Error.ForbiddenAccess",
    });

    await page.goto("/catalog/products");

    await expect(page).not.toHaveURL(/\/tenant-deactivated/);
  });
});
