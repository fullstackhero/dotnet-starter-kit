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

  test("uploads a new avatar and PUTs the durable URL to /identity/profile/image", async ({
    page,
  }) => {
    await mockJsonResponse(page, "**/api/v1/identity/profile", PROFILE);

    // The avatar editor (ImageInput) now uses the presigned-upload protocol
    // instead of inlining a base64 data: URL:
    //   1. POST /files/upload-url       → presigned PUT target + fileAssetId
    //   2. PUT  <uploadUrl>             → bytes pushed straight to storage
    //   3. POST /files/{id}/finalize    → server marks the asset Available
    //   4. GET  /files/{id}             → durable publicUrl
    // The page then PUTs { imageUrl: <publicUrl> } to /identity/profile/image.
    const FILE_ASSET_ID = "fa-avatar-1";
    const PUBLIC_URL = "https://cdn.example.test/avatars/fa-avatar-1.png";
    const ASSET = {
      id: FILE_ASSET_ID,
      ownerType: "User",
      ownerId: "u-test-1",
      originalFileName: "avatar.png",
      contentType: "image/png",
      sizeBytes: 8,
      visibility: "Public",
      status: "Available",
      scanStatus: 0,
      createdAtUtc: "2026-05-28T00:00:00Z",
      createdByUserId: "u-test-1",
      publicUrl: PUBLIC_URL,
    };

    await mockJsonResponse(page, "**/api/v1/files/upload-url", {
      fileAssetId: FILE_ASSET_ID,
      // Route the presigned PUT through a URL we can intercept below.
      uploadUrl: "https://storage.example.test/presigned/avatar.png",
      requiredHeaders: {},
      expiresAt: "2099-01-01T00:00:00Z",
    }, { method: "POST" });
    // The presigned PUT goes straight to storage (no FSH content-type needed);
    // just answer 200 so the upload step resolves.
    await page.route("https://storage.example.test/presigned/**", (route) =>
      route.fulfill({ status: 200, body: "" }),
    );
    await mockJsonResponse(page, `**/api/v1/files/${FILE_ASSET_ID}/finalize`, ASSET, {
      method: "POST",
    });
    // Metadata GET — must come after finalize so the POST glob doesn't swallow it.
    await mockJsonResponse(page, `**/api/v1/files/${FILE_ASSET_ID}`, ASSET, { method: "GET" });

    // The profile-image PUT. Body must be JSON-parseable even for a void
    // endpoint (api-client calls response.json() on a 200 JSON body) → "null".
    await mockJsonResponse(page, "**/api/v1/identity/profile/image", "null", { method: "PUT" });

    await page.goto("/settings/profile");

    const main = page.getByRole("main");
    // With no current image the upload affordance reads "Choose image".
    const upload = main.getByRole("button", { name: /choose image/i });
    await expect(upload).toBeVisible({ timeout: 10_000 });

    const reqPromise = page.waitForRequest(
      (r) => r.url().includes("/api/v1/identity/profile/image") && r.method() === "PUT",
      { timeout: 10_000 },
    );

    // The button creates a transient <input type=file> and clicks it, which
    // fires a filechooser — supply the file through that event.
    const chooserPromise = page.waitForEvent("filechooser");
    await upload.click();
    const chooser = await chooserPromise;
    await chooser.setFiles({
      name: "avatar.png",
      mimeType: "image/png",
      buffer: Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
    });

    const req = await reqPromise;
    const body = JSON.parse(req.postData() ?? "{}");
    // The page persists the durable publicUrl, not a base64 data: URL.
    expect(body.imageUrl).toBe(PUBLIC_URL);

    await expect(page.getByText(/profile image updated/i)).toBeVisible();
  });
});
