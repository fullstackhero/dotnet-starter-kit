import { expect, test } from "@playwright/test";
import { mockJsonResponse, mockProblemDetails } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installAdminShellMocks, ADMIN_PERMS } from "../helpers/shell-mocks";

// SecuritySettings = password change + 2FA. The 2FA branch is driven by the
// profile's twoFactorEnabled flag (GET /api/v1/identity/profile). Password
// change POSTs to /api/v1/identity/users/change-password with
// { password, newPassword, confirmNewPassword }.

const PROFILE_2FA_OFF = {
  id: "u-test-1",
  userName: "rootadmin",
  email: "admin@root.com",
  firstName: "Root",
  lastName: "Admin",
  isActive: true,
  emailConfirmed: true,
  twoFactorEnabled: false,
  imageUrl: null,
};

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, { ...TEST_USER, permissions: [...ADMIN_PERMS] });
  await installAdminShellMocks(page);
});

test.describe("settings · security", () => {
  test("renders the change-password form and the 2FA-off state", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/profile", PROFILE_2FA_OFF);

    await page.goto("/settings/security");

    const main = page.getByRole("main");
    // Section title renders with a literal "\\ " prefix; anchor to it to avoid
    // colliding with prose that also contains "password".
    await expect(main.getByText(/\\ Password/)).toBeVisible({ timeout: 10_000 });
    await expect(main.getByLabel(/^Current password/)).toBeVisible();
    await expect(main.getByLabel(/^New password/)).toBeVisible();
    await expect(main.getByLabel(/^Confirm new password/)).toBeVisible();

    // 2FA disabled → enroll affordance + "off" badge.
    await expect(main.getByRole("button", { name: /enable two-factor/i })).toBeVisible();
    await expect(main.getByText("off", { exact: true })).toBeVisible();
  });

  test("POSTs the change-password payload on submit", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/profile", PROFILE_2FA_OFF);
    await mockJsonResponse(
      page,
      "**/api/v1/identity/users/change-password",
      '"Password changed."',
      { method: "POST" },
    );

    await page.goto("/settings/security");

    const main = page.getByRole("main");
    await expect(main.getByLabel(/^Current password/)).toBeVisible({ timeout: 10_000 });

    await main.getByLabel(/^Current password/).fill("OldPass123!");
    await main.getByLabel(/^New password/).fill("NewPass456!");
    await main.getByLabel(/^Confirm new password/).fill("NewPass456!");

    const reqPromise = page.waitForRequest(
      (r) =>
        r.url().includes("/api/v1/identity/users/change-password") && r.method() === "POST",
      { timeout: 5_000 },
    );
    await main.getByRole("button", { name: /update password/i }).click();
    const req = await reqPromise;

    const body = JSON.parse(req.postData() ?? "{}");
    expect(body.password).toBe("OldPass123!");
    expect(body.newPassword).toBe("NewPass456!");
    expect(body.confirmNewPassword).toBe("NewPass456!");

    await expect(page.getByText(/password changed/i)).toBeVisible();
  });

  test("shows a validation error and does NOT POST when passwords mismatch", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/profile", PROFILE_2FA_OFF);

    await page.goto("/settings/security");

    const main = page.getByRole("main");
    await expect(main.getByLabel(/^Current password/)).toBeVisible({ timeout: 10_000 });

    await main.getByLabel(/^Current password/).fill("OldPass123!");
    await main.getByLabel(/^New password/).fill("NewPass456!");
    await main.getByLabel(/^Confirm new password/).fill("Different789!");

    let posted = false;
    page.on("request", (r) => {
      if (r.url().includes("/change-password") && r.method() === "POST") posted = true;
    });

    await main.getByRole("button", { name: /update password/i }).click();
    await expect(main.getByText(/passwords don't match/i)).toBeVisible();
    expect(posted).toBe(false);
  });

  test("renders the 2FA-enabled disable controls when twoFactorEnabled is true", async ({ page }) => {
    // Override the shell profile to flip the 2FA flag on.
    await mockJsonResponse(page, "**/api/v1/identity/profile", {
      ...PROFILE_2FA_OFF,
      twoFactorEnabled: true,
    });

    await page.goto("/settings/security");

    const main = page.getByRole("main");
    await expect(main.getByRole("button", { name: /disable two-factor/i })).toBeVisible({
      timeout: 10_000,
    });
    await expect(main.getByText("enabled", { exact: true })).toBeVisible();
  });

  test("surfaces a server error toast when change-password is rejected", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/profile", PROFILE_2FA_OFF);
    await mockProblemDetails(
      page,
      "**/api/v1/identity/users/change-password",
      400,
      { title: "Bad Request", detail: "Current password is incorrect." },
    );

    await page.goto("/settings/security");

    const main = page.getByRole("main");
    await expect(main.getByLabel(/^Current password/)).toBeVisible({ timeout: 10_000 });
    await main.getByLabel(/^Current password/).fill("WrongPass1!");
    await main.getByLabel(/^New password/).fill("NewPass456!");
    await main.getByLabel(/^Confirm new password/).fill("NewPass456!");
    await main.getByRole("button", { name: /update password/i }).click();

    await expect(page.getByText(/current password is incorrect/i)).toBeVisible();
  });
});
