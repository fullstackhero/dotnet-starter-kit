import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installAdminShellMocks, ADMIN_PERMS } from "../helpers/shell-mocks";

// ProfileSettings is read-only identity + an avatar editor. The page loads the
// current user via GET /api/v1/identity/profile and PUTs avatar changes to
// /api/v1/identity/profile/image. The installAdminShellMocks helper already
// stubs /identity/profile with ADMIN_PROFILE; tests that need different field
// values OVERRIDE that endpoint after the shell call (later route wins).

const PROFILE = {
  id: "u-test-1",
  userName: "rootadmin",
  email: "admin@root.com",
  firstName: "Root",
  lastName: "Admin",
  phoneNumber: "+1 555 0142",
  isActive: true,
  emailConfirmed: true,
  twoFactorEnabled: false,
  imageUrl: null,
};

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, { ...TEST_USER, permissions: [...ADMIN_PERMS] });
  await installAdminShellMocks(page);
});

test.describe("settings · profile", () => {
  test("renders the identity fields from the mocked profile", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/profile", PROFILE);

    await page.goto("/settings/profile");

    const main = page.getByRole("main");
    // Section titles render with a literal "\\ " prefix → match via regex.
    await expect(main.getByText(/Identity/)).toBeVisible({ timeout: 10_000 });
    await expect(main.getByText(/Avatar/)).toBeVisible();

    // Read-only identity inputs are surfaced via labelled fields.
    await expect(main.getByLabel(/^Username/)).toHaveValue("rootadmin");
    await expect(main.getByLabel(/^Display name/)).toHaveValue("Root Admin");
    await expect(main.getByLabel(/^Email/)).toHaveValue("admin@root.com");
    await expect(main.getByLabel(/^Phone/)).toHaveValue("+1 555 0142");
  });

  test("shows status badges driven by the profile flags", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/profile", PROFILE);

    await page.goto("/settings/profile");

    const main = page.getByRole("main");
    await expect(main.getByText(/Identity/)).toBeVisible({ timeout: 10_000 });
    await expect(main.getByText("Active", { exact: true })).toBeVisible();
    await expect(main.getByText("Email confirmed", { exact: true })).toBeVisible();
    await expect(main.getByText("2FA off", { exact: true })).toBeVisible();
  });

  test("reflects a disabled / unverified / 2FA-enabled profile", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/profile", {
      ...PROFILE,
      isActive: false,
      emailConfirmed: false,
      twoFactorEnabled: true,
    });

    await page.goto("/settings/profile");

    const main = page.getByRole("main");
    await expect(main.getByText("Disabled", { exact: true })).toBeVisible({ timeout: 10_000 });
    await expect(main.getByText("Email pending", { exact: true })).toBeVisible();
    await expect(main.getByText("2FA enabled", { exact: true })).toBeVisible();
  });

  test("uploads a new avatar via PUT /identity/profile/image", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/profile", PROFILE);
    // Override the avatar PUT after the shell so it wins for this method.
    // Body must be JSON-parseable even for a void endpoint (api-client calls
    // response.json() on any 200 with a JSON content-type) → return "null".
    await mockJsonResponse(page, "**/api/v1/identity/profile/image", "null", { method: "PUT" });

    await page.goto("/settings/profile");

    const main = page.getByRole("main");
    const upload = main.getByRole("button", { name: /upload new/i });
    await expect(upload).toBeVisible({ timeout: 10_000 });

    const reqPromise = page.waitForRequest(
      (r) => r.url().includes("/api/v1/identity/profile/image") && r.method() === "PUT",
      { timeout: 5_000 },
    );

    // The visible button proxies a hidden <input type=file> — set files directly.
    await main.locator('input[type="file"]').setInputFiles({
      name: "avatar.png",
      mimeType: "image/png",
      buffer: Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
    });

    const req = await reqPromise;
    const body = JSON.parse(req.postData() ?? "{}");
    // The page inlines the file as a data: URL into { imageUrl }.
    expect(body.imageUrl).toMatch(/^data:image\/png/);

    await expect(page.getByText(/avatar updated/i)).toBeVisible();
  });
});
