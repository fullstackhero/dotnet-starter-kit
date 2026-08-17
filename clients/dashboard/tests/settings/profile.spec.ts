import { expect, test } from "@playwright/test";
import { mockJsonResponse, mockProblemDetails } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";

const PROFILE = {
  id: TEST_USER.sub,
  userName: "alice",
  email: TEST_USER.email,
  firstName: TEST_USER.firstName,
  lastName: TEST_USER.lastName,
  phoneNumber: "",
  isActive: true,
  emailConfirmed: true,
  twoFactorEnabled: false,
};

// All settings tests need an authed session and a mocked profile fetch.
test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, TEST_USER);
  await mockJsonResponse(page, "**/api/v1/identity/profile", PROFILE);
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

  // The dashboard talks to the API cross-origin in dev, and `ETag` is not a CORS-safelisted
  // response header — the browser hides it from JS unless the server also sends
  // `Access-Control-Expose-Headers: ETag`. These mocks mirror what the CORS policy now sends;
  // without it the client reads `null` and silently stops sending `If-Match`. The server side of
  // that contract is asserted by `GetProfile_Should_ExposeETagToCrossOriginCallers_When_ProfileIsRead`,
  // since a mock alone would keep passing if the policy stopped exposing the header.
  const ETAG_CORS_HEADERS = {
    "Content-Type": "application/json",
    "Access-Control-Expose-Headers": "ETag",
  } as const;

  test("echoes the profile ETag back as If-Match on save", async ({ page }) => {
    const etag = '"stamp-1"';
    await page.route("**/api/v1/identity/profile", async (route) => {
      if (route.request().method() === "PUT") {
        await route.fulfill({
          status: 200,
          headers: { "Content-Type": "application/json" },
          body: '""',
        });
        return;
      }
      await route.fulfill({
        status: 200,
        headers: { ...ETAG_CORS_HEADERS, ETag: etag },
        body: JSON.stringify(PROFILE),
      });
    });

    await page.goto("/settings/profile");
    await expect(page.getByLabel("First name")).toHaveValue("Alice");

    await page.getByLabel("First name").fill("Alicia");

    const putReqPromise = page.waitForRequest(
      (req) =>
        req.url().includes("/api/v1/identity/profile") &&
        req.method() === "PUT" &&
        !req.url().includes("/image"),
      { timeout: 5_000 },
    );
    await page.getByRole("button", { name: /save changes/i }).click();
    const putReq = await putReqPromise;

    // Without this the server cannot tell a deliberate overwrite from a lost update.
    expect(putReq.headers()["if-match"]).toBe(etag);
  });

  test("refetches and retries once when the save is rejected with 412", async ({ page }) => {
    // The token also rotates on writes the user never sees as profile edits (a password
    // change, a failed sign-in, a new avatar), so a single 412 has to resolve itself
    // against a fresh read instead of surfacing as a failed save.
    const sentIfMatch: string[] = [];
    let getCount = 0;

    await page.route("**/api/v1/identity/profile", async (route) => {
      const request = route.request();
      if (request.method() === "PUT") {
        sentIfMatch.push(request.headers()["if-match"] ?? "");
        if (sentIfMatch.length === 1) {
          await route.fulfill({
            status: 412,
            headers: { "Content-Type": "application/problem+json" },
            body: JSON.stringify({
              status: 412,
              title: "CustomException",
              detail: "The profile changed since you loaded it.",
            }),
          });
          return;
        }
        await route.fulfill({
          status: 200,
          headers: { "Content-Type": "application/json" },
          body: '""',
        });
        return;
      }

      // Every read hands out a fresh token, so the retry provably carries a re-read one.
      getCount += 1;
      await route.fulfill({
        status: 200,
        headers: { ...ETAG_CORS_HEADERS, ETag: `"stamp-${getCount}"` },
        body: JSON.stringify(PROFILE),
      });
    });

    await page.goto("/settings/profile");
    await expect(page.getByLabel("First name")).toHaveValue("Alice");

    await page.getByLabel("First name").fill("Alicia");
    await page.getByRole("button", { name: /save changes/i }).click();

    await expect(page.getByText(/profile saved/i)).toBeVisible();
    await expect(page.getByText(/save failed/i)).toBeHidden();
    expect(sentIfMatch).toHaveLength(2);
    expect(sentIfMatch[0]).not.toBe("");
    expect(sentIfMatch[1]).not.toBe(sentIfMatch[0]);
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
