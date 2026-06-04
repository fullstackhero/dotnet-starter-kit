import { expect, test } from "@playwright/test";
import { mockJsonResponse, mockProblemDetails } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";

function profile2fa(enabled: boolean) {
  return {
    id: TEST_USER.sub,
    userName: "alice",
    email: TEST_USER.email,
    firstName: TEST_USER.firstName,
    lastName: TEST_USER.lastName,
    phoneNumber: "",
    isActive: true,
    emailConfirmed: true,
    twoFactorEnabled: enabled,
  };
}

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, TEST_USER);
  // Sessions endpoint isn't the focus here — return an empty list so
  // the SessionsCard renders its empty state without 401-ing.
  await mockJsonResponse(page, "**/api/v1/identity/sessions/me", []);
});

test.describe("settings/security — change password (Dialog)", () => {
  test.beforeEach(async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/profile", profile2fa(false));
  });

  test("opens the dialog when 'Change password' is clicked", async ({ page }) => {
    await page.goto("/settings/security");
    await page.getByRole("button", { name: /change password/i }).click();

    await expect(
      page.getByRole("heading", { name: /change password/i, level: 2 }),
    ).toBeVisible();
    await expect(page.getByLabel("Current password")).toBeFocused();
  });

  test("client-side validation: empty/short/mismatched", async ({ page }) => {
    await page.goto("/settings/security");
    await page.getByRole("button", { name: /change password/i }).click();

    // Scope getByLabel into the dialog so the on-page password card
    // (and any other "Password" label on the page) doesn't ambiguate
    // — Radix Dialog announces role="dialog".
    const dialog = page.getByRole("dialog");

    await dialog.getByLabel("Current password").fill("oldsecret");
    await dialog.getByLabel("New password", { exact: true }).fill("short");
    await dialog.getByLabel("Confirm new password").fill("short");
    await dialog.getByRole("button", { name: /update password/i }).click();
    await expect(dialog.getByText(/at least 8 characters/i)).toBeVisible();

    await dialog.getByLabel("New password", { exact: true }).fill("longenough123");
    await dialog.getByLabel("Confirm new password").fill("differentpw123");
    await dialog.getByRole("button", { name: /update password/i }).click();
    // Scope to the submit-error alert — the live "…don't match yet" hint under
    // the confirm field also contains this phrase.
    await expect(dialog.getByRole("alert")).toContainText(/passwords don't match/i);

    // Current === new is blocked — keeps users from "rotating" to the
    // same value just to reset a forgotten password.
    await dialog.getByLabel("Current password").fill("longenough123");
    await dialog.getByLabel("New password", { exact: true }).fill("longenough123");
    await dialog.getByLabel("Confirm new password").fill("longenough123");
    await dialog.getByRole("button", { name: /update password/i }).click();
    await expect(dialog.getByText(/must differ/i)).toBeVisible();
  });

  test("POSTs to /change-password on success and closes the dialog", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/change-password", '""');

    await page.goto("/settings/security");
    await page.getByRole("button", { name: /change password/i }).click();

    const dialog = page.getByRole("dialog");
    await dialog.getByLabel("Current password").fill("oldsecret123");
    await dialog.getByLabel("New password", { exact: true }).fill("newSecret!1234");
    await dialog.getByLabel("Confirm new password").fill("newSecret!1234");

    const reqPromise = page.waitForRequest(
      (r) =>
        r.url().includes("/api/v1/identity/change-password") && r.method() === "POST",
      { timeout: 5_000 },
    );
    await dialog.getByRole("button", { name: /update password/i }).click();
    const req = await reqPromise;

    expect(JSON.parse(req.postData() ?? "{}")).toMatchObject({
      password: "oldsecret123",
      newPassword: "newSecret!1234",
      confirmNewPassword: "newSecret!1234",
    });

    await expect(page.getByText(/password changed/i)).toBeVisible();
    // Dialog closes on success.
    await expect(page.getByRole("dialog")).not.toBeVisible();
  });

  test("surfaces server errors inside the dialog without closing", async ({ page }) => {
    await mockProblemDetails(page, "**/api/v1/identity/change-password", 400, {
      title: "Invalid password",
      detail: "Current password is incorrect.",
    });

    await page.goto("/settings/security");
    await page.getByRole("button", { name: /change password/i }).click();

    const dialog = page.getByRole("dialog");
    await dialog.getByLabel("Current password").fill("wrongsecret");
    await dialog.getByLabel("New password", { exact: true }).fill("newSecret!1234");
    await dialog.getByLabel("Confirm new password").fill("newSecret!1234");
    await dialog.getByRole("button", { name: /update password/i }).click();

    await expect(dialog.getByText(/current password is incorrect/i)).toBeVisible();
    // Dialog stays open on error so user can correct without re-entering.
    await expect(dialog).toBeVisible();
  });
});

test.describe("settings/security — two-factor enroll (disabled → enabled)", () => {
  test.beforeEach(async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/profile", profile2fa(false));
  });

  test("shows the 'Enable two-factor' affordance when 2FA is off", async ({ page }) => {
    await page.goto("/settings/security");

    // CardTitle in the shadcn-style Card is a styled <div>, not a heading.
    // The primary affordance is the button — assert on that directly.
    await expect(page.getByRole("button", { name: /enable two-factor/i })).toBeVisible();
    // The "disabled" badge inside the card title is also a reliable signal.
    await expect(page.getByText(/^disabled$/i)).toBeVisible();
  });

  test("clicking enable triggers POST /2fa/enroll and reveals the QR + manual key", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/2fa/enroll", {
      sharedKey: "ABCD-EFGH-IJKL-MNOP",
      authenticatorUri: "otpauth://totp/FSH:alice?secret=ABCDEFGHIJKLMNOP&issuer=FSH",
    });

    await page.goto("/settings/security");
    await page.getByRole("button", { name: /enable two-factor/i }).click();

    // QR container + manual key + verification input render.
    await expect(page.getByRole("img", { name: /two-factor qr code/i })).toBeVisible();
    await expect(page.getByText("ABCD-EFGH-IJKL-MNOP")).toBeVisible();
    await expect(page.getByLabel(/6-digit code/i)).toBeVisible();
  });

  test("submitting the 6-digit code enables 2FA and refetches profile", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/2fa/enroll", {
      sharedKey: "ABCD-EFGH-IJKL-MNOP",
      authenticatorUri: "otpauth://totp/FSH:alice?secret=AAAAAA",
    });
    await mockJsonResponse(page, "**/api/v1/identity/2fa/verify", { success: true });

    await page.goto("/settings/security");
    await page.getByRole("button", { name: /enable two-factor/i }).click();

    const code = page.getByLabel(/6-digit code/i);
    await code.fill("12 34 56");

    const reqPromise = page.waitForRequest(
      (r) =>
        r.url().includes("/api/v1/identity/2fa/verify") && r.method() === "POST",
      { timeout: 5_000 },
    );
    await page.getByRole("button", { name: /confirm & enable/i }).click();
    const req = await reqPromise;

    // Whitespace stripped before submit (the handler does .replace(/\s/g, "")).
    expect(JSON.parse(req.postData() ?? "{}")).toMatchObject({ code: "123456" });
    await expect(page.getByText(/two-factor enabled/i)).toBeVisible();
  });

  test("disabled 'Confirm' button until 6+ characters entered", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/2fa/enroll", {
      sharedKey: "ABCD",
      authenticatorUri: "otpauth://totp/FSH:alice?secret=AAAAAA",
    });

    await page.goto("/settings/security");
    await page.getByRole("button", { name: /enable two-factor/i }).click();
    const confirm = page.getByRole("button", { name: /confirm & enable/i });
    await expect(confirm).toBeDisabled();

    await page.getByLabel(/6-digit code/i).fill("123");
    await expect(confirm).toBeDisabled();

    await page.getByLabel(/6-digit code/i).fill("123456");
    await expect(confirm).toBeEnabled();
  });
});

test.describe("settings/security — two-factor disable", () => {
  test.beforeEach(async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/profile", profile2fa(true));
  });

  test("renders the disable form when 2FA is already enabled", async ({ page }) => {
    await page.goto("/settings/security");
    await expect(page.getByLabel("Current password")).toBeVisible();
    await expect(page.getByRole("button", { name: /disable two-factor/i })).toBeVisible();
  });

  test("POST /2fa/disable with the current password, then refetches profile", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/2fa/disable", { success: true });

    await page.goto("/settings/security");
    await page.getByLabel("Current password").fill("mypassword");

    const reqPromise = page.waitForRequest(
      (r) =>
        r.url().includes("/api/v1/identity/2fa/disable") && r.method() === "POST",
      { timeout: 5_000 },
    );
    await page.getByRole("button", { name: /disable two-factor/i }).click();
    const req = await reqPromise;

    expect(JSON.parse(req.postData() ?? "{}")).toMatchObject({
      currentPassword: "mypassword",
    });
    await expect(page.getByText(/two-factor disabled/i)).toBeVisible();
  });
});
