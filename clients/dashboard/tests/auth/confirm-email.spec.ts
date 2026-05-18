import { expect, test } from "@playwright/test";
import { mockJsonResponse, mockProblemDetails } from "../helpers/api-mocks";

const VALID_LINK =
  "/confirm-email?userId=u-1&code=verify-code&tenant=acme";

test.describe("confirm-email page", () => {
  test("renders the malformed-link state when params are missing", async ({ page }) => {
    await page.goto("/confirm-email");
    await expect(
      page.getByRole("heading", { name: /couldn't confirm your email/i }),
    ).toBeVisible();
    await expect(
      page.getByText(/missing required parameters/i),
    ).toBeVisible();
  });

  test("auto-fires the GET on mount and shows the success state on 2xx", async ({ page }) => {
    await mockJsonResponse(
      page,
      "**/api/v1/identity/confirm-email**",
      '"Your email is confirmed."',
    );

    await page.goto(VALID_LINK);

    await expect(page.getByRole("heading", { name: /email confirmed/i })).toBeVisible();
    await expect(page.getByText(/your email is confirmed/i)).toBeVisible();
    await expect(page.getByRole("link", { name: /continue to sign in/i })).toBeVisible();
  });

  test("surfaces server errors as an actionable failure state", async ({ page }) => {
    await mockProblemDetails(page, "**/api/v1/identity/confirm-email**", 400, {
      title: "Invalid token",
      detail: "The confirmation token is no longer valid.",
    });

    await page.goto(VALID_LINK);

    await expect(
      page.getByRole("heading", { name: /couldn't confirm/i }),
    ).toBeVisible();
    await expect(page.getByText(/no longer valid/i)).toBeVisible();

    // The error state offers BOTH "back to sign in" and "reset password" —
    // a stuck user has a recovery path. The footer also renders a
    // "← Back to sign in" link, so we exact-match the in-card button.
    await expect(page.getByRole("link", { name: "Back to sign in", exact: true })).toBeVisible();
    await expect(page.getByRole("link", { name: /reset password instead/i })).toBeVisible();
  });

  test("'continue to sign in' link routes to /login", async ({ page }) => {
    await mockJsonResponse(
      page,
      "**/api/v1/identity/confirm-email**",
      '"Your email is confirmed."',
    );

    await page.goto(VALID_LINK);
    await page.getByRole("link", { name: /continue to sign in/i }).click();
    await expect(page).toHaveURL(/\/login$/);
  });
});
