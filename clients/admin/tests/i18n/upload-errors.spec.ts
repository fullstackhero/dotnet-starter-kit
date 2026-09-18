import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installAdminShellMocks, ADMIN_PERMS } from "../helpers/shell-mocks";

// Upload failures are raised deep in the hook, where no translator is in scope, so they travel as
// catalog keys on an UploadError and are resolved at the toast by describeUploadError. Nothing in
// the suite walked that path end to end: the keys could be missing, namespaced wrong, or carry the
// wrong interpolation params and every spec stayed green while a Portuguese operator read English.
//
// The avatar editor on /settings/profile is the admin mount of ImageInput, and its picker is an
// input created in JS and clicked programmatically — reachable through the filechooser event, not
// through a selector.

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

const STORAGE_URL = "https://storage.test.invalid/bucket/avatar.png";

const PRESIGNED = {
  fileAssetId: "fa-1",
  uploadUrl: STORAGE_URL,
  requiredHeaders: {},
  expiresAt: "2026-05-01T12:15:00Z",
};

/** A one-pixel PNG, so the extension and size checks in the hook let the upload start. */
const PNG_BYTES = Buffer.from(
  "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==",
  "base64",
);

async function pickAvatar(page: import("@playwright/test").Page) {
  await page.goto("/settings/profile?culture=pt-BR");
  await expect(page.locator("html")).toHaveAttribute("lang", "pt-BR");

  const chooser = page.waitForEvent("filechooser");
  await page.getByRole("button", { name: "Escolher imagem" }).click();
  await (await chooser).setFiles({ name: "avatar.png", mimeType: "image/png", buffer: PNG_BYTES });
}

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, { ...TEST_USER, permissions: [...ADMIN_PERMS] });
  await installAdminShellMocks(page);
  await mockJsonResponse(page, "**/api/v1/identity/profile", PROFILE);
});

test.describe("upload failures are localized", () => {
  test("a storage PUT that never connects reports a network error in Portuguese", async ({
    page,
  }) => {
    await mockJsonResponse(page, "**/api/v1/files/upload-url", PRESIGNED);
    // Aborting is what xhr.onerror is for; the hook turns it into common:upload.networkError.
    await page.route(STORAGE_URL, (route) => route.abort());

    await pickAvatar(page);

    await expect(page.getByText("Erro de rede durante o envio.")).toBeVisible();
  });

  test("a storage PUT the bucket rejects reports the status it came back with", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/files/upload-url", PRESIGNED);
    // The interpolated {{status}} is the half a key-only assertion would miss: the catalog entry
    // renders fine with the placeholder empty, and the operator loses the only diagnostic.
    await page.route(STORAGE_URL, (route) => route.fulfill({ status: 403, body: "" }));

    await pickAvatar(page);

    await expect(page.getByText(/^Envio recusado pelo armazenamento \(403/)).toBeVisible();
  });

  test("a presign request that never reaches the API falls back to the catalog, not to the platform", async ({
    page,
  }) => {
    // apiFetch does not wrap fetch, so this surfaces as TypeError("Failed to fetch"). Returning
    // e.message put that English string on screen, ahead of the localized fallback the caller had
    // already passed in; the catalog string is what belongs there.
    await page.route("**/api/v1/files/upload-url", (route) => route.abort());

    await pickAvatar(page);

    await expect(page.getByText("Falha no envio")).toBeVisible();
    await expect(page.getByText(/Failed to fetch/)).toHaveCount(0);
  });
});
