import { expect, test } from "@playwright/test";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installAdminShellMocks, ADMIN_PERMS, paged } from "../helpers/shell-mocks";
import { mockJsonResponse } from "../helpers/api-mocks";

const SUB = {
  id: "wh-1111",
  url: "https://hooks.example.com/fsh",
  events: ["tenant.created", "user.registered"],
  isActive: true,
  createdAtUtc: "2026-05-10T09:00:00Z",
};

const DELIVERY = {
  id: "dlv-1",
  subscriptionId: SUB.id,
  eventType: "tenant.created",
  httpStatusCode: 200,
  success: true,
  attemptCount: 1,
  attemptedAtUtc: "2026-05-20T12:00:00Z",
  errorMessage: null,
};

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, { ...TEST_USER, permissions: [...ADMIN_PERMS] });
  await installAdminShellMocks(page);
});

test.describe("webhooks subscriptions list", () => {
  test("renders the heading and a subscription row from the mock", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/webhooks/subscriptions?*", paged([SUB]));

    await page.goto("/webhooks");

    const main = page.getByRole("main");
    await expect(
      main.getByRole("heading", { name: "Webhooks", exact: true }),
    ).toBeVisible({ timeout: 10_000 });

    await expect(main.getByText(SUB.url, { exact: true })).toBeVisible();
    await expect(main.getByText("tenant.created", { exact: true })).toBeVisible();
    // New subscription button present.
    await expect(main.getByRole("button", { name: /new subscription/i })).toBeVisible();
  });

  test("shows the empty state when there are no subscriptions", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/webhooks/subscriptions?*", paged([]));

    await page.goto("/webhooks");

    const main = page.getByRole("main");
    await expect(
      main.getByText("No webhook subscriptions yet.", { exact: true }),
    ).toBeVisible({ timeout: 10_000 });
    await expect(
      main.getByText(
        "Add an endpoint and pick which events should fire. We'll retry failed deliveries automatically.",
        { exact: true },
      ),
    ).toBeVisible();
  });
});

test.describe("webhook detail (deliveries)", () => {
  test("loads the endpoint sections and a delivery row", async ({ page }) => {
    // Detail finds the sub by listing subscriptions (page 1, big page size).
    await mockJsonResponse(page, "**/api/v1/webhooks/subscriptions?*", paged([SUB], { pageSize: 200 }));
    await mockJsonResponse(
      page,
      `**/api/v1/webhooks/subscriptions/${SUB.id}/deliveries?*`,
      paged([DELIVERY]),
    );

    await page.goto(`/webhooks/${SUB.id}`);

    const main = page.getByRole("main");
    // h1 is the subscription URL.
    await expect(
      main.getByRole("heading", { name: SUB.url, exact: true }),
    ).toBeVisible({ timeout: 10_000 });

    // Section titles now render via SettingsSection (h2 with plain titles).
    await expect(
      main.getByRole("heading", { name: "Endpoint", exact: true }),
    ).toBeVisible();
    await expect(
      main.getByRole("heading", { name: "Deliveries", exact: true }),
    ).toBeVisible();

    // Delivery row: event type chip + HTTP status badge.
    await expect(main.getByText("tenant.created", { exact: true }).first()).toBeVisible();
    await expect(main.getByText(/HTTP 200/)).toBeVisible();
  });

  test("shows the no-deliveries copy when the subscription has none", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/webhooks/subscriptions?*", paged([SUB], { pageSize: 200 }));
    await mockJsonResponse(
      page,
      `**/api/v1/webhooks/subscriptions/${SUB.id}/deliveries?*`,
      paged([]),
    );

    await page.goto(`/webhooks/${SUB.id}`);

    const main = page.getByRole("main");
    await expect(
      main.getByText(
        "No deliveries yet. Try the test button above, or wait for matching events to fire.",
        { exact: true },
      ),
    ).toBeVisible({ timeout: 10_000 });
  });
});
