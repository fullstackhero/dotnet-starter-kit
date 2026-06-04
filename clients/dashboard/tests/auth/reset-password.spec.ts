import { expect, test } from "@playwright/test";
import { captureRequest, mockJsonResponse, mockProblemDetails } from "../helpers/api-mocks";

// Helper — builds the URL the email link would produce.
const VALID_LINK =
  "/reset-password?token=ABC123def&email=alice@acme.com&tenant=acme";

test.describe("reset-password page", () => {
  test("guards against malformed links (missing token/email/tenant)", async ({ page }) => {
    await page.goto("/reset-password");
    await expect(page.getByRole("heading", { name: /this link is incomplete/i })).toBeVisible();
    // Should offer a recovery path, not a dead-end.
    await expect(page.getByRole("link", { name: /request a new link/i })).toBeVisible();
  });

  test("renders the form when token + email + tenant are present", async ({ page }) => {
    await page.goto(VALID_LINK);

    // The redesigned AuthShell drops the editorial eyebrow — assert on
    // the visible headline + echoed email/tenant + field labels.
    await expect(page.getByRole("heading", { name: /set a new password/i })).toBeVisible();
    await expect(page.getByText("alice@acme.com")).toBeVisible();
    await expect(page.getByText(/on acme/i)).toBeVisible();
    await expect(page.getByLabel("New password")).toBeVisible();
    await expect(page.getByLabel("Confirm password")).toBeVisible();
  });

  test("password strength meter reflects what the user types", async ({ page }) => {
    await page.goto(VALID_LINK);

    const newPw = page.getByLabel("New password");

    await newPw.fill("short");
    await expect(page.getByText(/^Weak$/)).toBeVisible();

    await newPw.fill("Abcdefg1");
    await expect(page.getByText(/^Fair$/)).toBeVisible();

    await newPw.fill("VeryStrong!Passw0rd");
    await expect(page.getByText(/^Strong$/)).toBeVisible();
  });

  test("match indicator turns green when confirm matches", async ({ page }) => {
    await page.goto(VALID_LINK);

    await page.getByLabel("New password").fill("VeryStrong!Passw0rd");
    await page.getByLabel("Confirm password").fill("VeryStrong!Pas");
    await expect(page.getByText(/doesn't match yet/i)).toBeVisible();

    await page.getByLabel("Confirm password").fill("VeryStrong!Passw0rd");
    await expect(page.getByText(/passwords match/i)).toBeVisible();
  });

  test("disables submit until the form is valid", async ({ page }) => {
    await page.goto(VALID_LINK);
    const submit = page.getByRole("button", { name: /set new password/i });

    await expect(submit).toBeDisabled();

    // Strong + matching unlocks submit.
    await page.getByLabel("New password").fill("VeryStrong!Passw0rd");
    await page.getByLabel("Confirm password").fill("VeryStrong!Passw0rd");
    await expect(submit).toBeEnabled();
  });

  test("posts to /reset-password and redirects to /login on success", async ({ page }) => {
    const captured = captureRequest(page, "**/api/v1/identity/reset-password");

    await page.goto(VALID_LINK);
    await page.getByLabel("New password").fill("VeryStrong!Passw0rd");
    await page.getByLabel("Confirm password").fill("VeryStrong!Passw0rd");
    await page.getByRole("button", { name: /set new password/i }).click();

    const { body, headers } = await captured.value();
    expect(body).toMatchObject({
      email: "alice@acme.com",
      password: "VeryStrong!Passw0rd",
      token: "ABC123def",
    });
    expect(headers.tenant).toBe("acme");

    // Redirects to /login on success.
    await expect(page).toHaveURL(/\/login$/);
  });

  test("surfaces a token-expired error inline without leaving the page", async ({ page }) => {
    await mockProblemDetails(page, "**/api/v1/identity/reset-password", 400, {
      title: "Invalid token",
      detail: "The reset token has expired or already been used.",
    });

    await page.goto(VALID_LINK);
    await page.getByLabel("New password").fill("VeryStrong!Passw0rd");
    await page.getByLabel("Confirm password").fill("VeryStrong!Passw0rd");
    await page.getByRole("button", { name: /set new password/i }).click();

    const alert = page.getByRole("alert");
    await expect(alert).toBeVisible();
    await expect(alert).toContainText(/token has expired/i);

    // Still on the reset form.
    await expect(page).toHaveURL(/\/reset-password/);
  });

  test("typing after an error clears the alert (avoids stale messages)", async ({ page }) => {
    await mockProblemDetails(page, "**/api/v1/identity/reset-password", 400, {
      title: "Invalid token",
      detail: "The reset token has expired or already been used.",
    });

    await page.goto(VALID_LINK);
    await page.getByLabel("New password").fill("VeryStrong!Passw0rd");
    await page.getByLabel("Confirm password").fill("VeryStrong!Passw0rd");
    await page.getByRole("button", { name: /set new password/i }).click();
    await expect(page.getByRole("alert")).toBeVisible();

    // Any keystroke in either password field should clear the alert.
    await page.getByLabel("New password").fill("VeryStrong!Passw0rd2");
    await expect(page.getByRole("alert")).not.toBeVisible();

    // Avoid unused-variable lint by ensuring mockJsonResponse is also
    // available; not asserted, just imported so this file demonstrates
    // both helper patterns.
    void mockJsonResponse;
  });
});
