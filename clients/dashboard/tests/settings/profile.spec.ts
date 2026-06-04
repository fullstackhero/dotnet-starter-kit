import { expect, test } from "@playwright/test";
import { mockJsonResponse, mockProblemDetails } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";

// All settings tests need an authed session and a mocked profile fetch.
test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, TEST_USER);
  await mockJsonResponse(page, "**/api/v1/identity/profile", {
    id: TEST_USER.sub,
    userName: "alice",
    email: TEST_USER.email,
    firstName: TEST_USER.firstName,
    lastName: TEST_USER.lastName,
    phoneNumber: "",
    isActive: true,
    emailConfirmed: true,
    twoFactorEnabled: false,
  });
});

test.describe("settings/profile — wired to PUT /identity/profile", () => {
  test("seeds form fields from the GET /profile response", async ({ page }) => {
    await page.goto("/settings/profile");

    // Wait for query-driven hydration (initial paint shows empty fields,
    // then useEffect copies the fetched profile in).
    await expect(page.getByLabel("First name")).toHaveValue("Alice");
    await expect(page.getByLabel("Last name")).toHaveValue("Nguyen");
    await expect(page.getByLabel("Email")).toHaveValue("alice@acme.com");
    await expect(page.getByLabel("Email")).toBeDisabled();
  });

  test("Save is disabled until the form is dirty", async ({ page }) => {
    await page.goto("/settings/profile");
    await expect(page.getByLabel("First name")).toHaveValue("Alice");

    const save = page.getByRole("button", { name: /save changes/i });
    await expect(save).toBeDisabled();

    await page.getByLabel("First name").fill("Alicia");
    await expect(save).toBeEnabled();
  });

  test("POSTing the form sends the new fields to PUT /profile", async ({ page }) => {
    // Don't use captureRequest here — it would also intercept the GET
    // that updateMyProfile fires as a pre-read for unchanged fields,
    // returning `""` and breaking the build-PUT step. Instead let the
    // beforeEach GET mock satisfy the pre-read, mock the PUT explicitly,
    // and grab its body via page.waitForRequest.
    await mockJsonResponse(page, "**/api/v1/identity/profile", '""', { method: "PUT" });

    await page.goto("/settings/profile");
    await expect(page.getByLabel("First name")).toHaveValue("Alice");

    await page.getByLabel("First name").fill("Alicia");
    await page.getByLabel("Last name").fill("Nguyen-Ortiz");
    await page.getByLabel("Phone").fill("+1 (555) 000-1234");

    // Start listening BEFORE the click — waitForRequest registers the
    // listener at the moment of the call, so it would miss a request
    // that fires synchronously inside the click handler.
    const putReqPromise = page.waitForRequest(
      (req) =>
        req.url().includes("/api/v1/identity/profile") &&
        req.method() === "PUT" &&
        !req.url().includes("/image"),
      { timeout: 5_000 },
    );
    await page.getByRole("button", { name: /save changes/i }).click();
    const putReq = await putReqPromise;

    const body = JSON.parse(putReq.postData() ?? "{}");
    expect(body).toMatchObject({
      id: TEST_USER.sub,
      firstName: "Alicia",
      lastName: "Nguyen-Ortiz",
      phoneNumber: "+1 (555) 000-1234",
      email: TEST_USER.email,
    });
  });

  test("shows a success toast on 200", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/profile", '""', { method: "PUT" });

    await page.goto("/settings/profile");
    await expect(page.getByLabel("First name")).toHaveValue("Alice");

    await page.getByLabel("First name").fill("Alicia");
    await page.getByRole("button", { name: /save changes/i }).click();

    await expect(page.getByText(/profile saved/i)).toBeVisible();
  });

  test("surfaces a destructive toast on server error", async ({ page }) => {
    await mockProblemDetails(page, "**/api/v1/identity/profile", 400, {
      title: "Validation failed",
      detail: "First name cannot be empty.",
    });

    await page.goto("/settings/profile");
    await expect(page.getByLabel("First name")).toHaveValue("Alice");

    await page.getByLabel("First name").fill("Alicia");
    await page.getByRole("button", { name: /save changes/i }).click();

    await expect(page.getByText(/save failed/i)).toBeVisible();
    await expect(page.getByText(/first name cannot be empty/i)).toBeVisible();
  });

  test("Reset button reverts edits to the original profile values", async ({ page }) => {
    await page.goto("/settings/profile");
    await expect(page.getByLabel("First name")).toHaveValue("Alice");

    await page.getByLabel("First name").fill("Alicia");
    await page.getByLabel("Phone").fill("+1 (555) 999-9999");

    await page.getByRole("button", { name: /^reset$/i }).click();

    await expect(page.getByLabel("First name")).toHaveValue("Alice");
    await expect(page.getByLabel("Phone")).toHaveValue("");
  });
});
