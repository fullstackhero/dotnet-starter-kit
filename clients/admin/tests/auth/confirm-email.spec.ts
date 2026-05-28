import { expect, test } from "@playwright/test";
import { mockJsonResponse, mockProblemDetails } from "../helpers/api-mocks";

const VALID_LINK = "/confirm-email?userId=u-1&code=verify-code&tenant=root";

test.describe("admin confirm-email", () => {
  test("malformed-link state when params are missing", async ({ page }) => {
    await page.goto("/confirm-email");
    await expect(page.getByText(/missing required parameters/i)).toBeVisible();
    await expect(page.getByRole("heading", { name: /couldn't confirm your email/i })).toBeVisible();
  });

  test("success state on 2xx with continue-to-signin CTA", async ({ page }) => {
    await mockJsonResponse(
      page,
      "**/api/v1/identity/confirm-email**",
      '"Your email is confirmed."',
    );
    await page.goto(VALID_LINK);

    await expect(page.getByText(/your email is confirmed/i)).toBeVisible();
    const cta = page.getByRole("link", { name: /continue to sign in/i });
    await expect(cta).toBeVisible();
    await cta.click();
    await expect(page).toHaveURL(/\/login$/);
  });

  test("failure state with recovery affordances", async ({ page }) => {
    await mockProblemDetails(page, "**/api/v1/identity/confirm-email**", 400, {
      title: "Invalid token",
      detail: "The confirmation token is no longer valid.",
    });
    await page.goto(VALID_LINK);

    await expect(page.getByText(/no longer valid/i)).toBeVisible();
    await expect(page.getByRole("link", { name: "Back to sign in", exact: true })).toBeVisible();
    await expect(page.getByRole("link", { name: /reset password instead/i })).toBeVisible();
  });
});
