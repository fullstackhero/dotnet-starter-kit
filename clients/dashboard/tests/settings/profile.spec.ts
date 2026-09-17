import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
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
    // The 400 belongs to the PUT. The GET has to keep working: the save is built from the
    // profile that read returned, so failing it would test "cannot save yet", not "save failed".
    await page.route("**/api/v1/identity/profile", async (route) => {
      if (route.request().method() !== "PUT") {
        await route.fallback();
        return;
      }
      await route.fulfill({
        status: 400,
        headers: { "Content-Type": "application/problem+json" },
        body: JSON.stringify({
          type: "https://httpstatuses.io/400",
          title: "Validation failed",
          status: 400,
          detail: "First name cannot be empty.",
        }),
      });
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

  test("the If-Match comes from the read that seeded the form, not from a read at save time", async ({
    page,
  }) => {
    // The lost update happens between the user seeing the values and pressing save. A tag read
    // inside the save is always current by construction, so it matches whatever the other writer
    // just stored and the overwrite goes through. Here the server moves on after the form is
    // seeded: the save must still carry the seeded tag, which is what lets the server say 412.
    const sentIfMatch: string[] = [];
    let currentStamp = 1;

    await page.route("**/api/v1/identity/profile", async (route) => {
      const request = route.request();
      if (request.method() === "PUT") {
        sentIfMatch.push(request.headers()["if-match"] ?? "");
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
        headers: { ...ETAG_CORS_HEADERS, ETag: `"stamp-${currentStamp}"` },
        body: JSON.stringify(PROFILE),
      });
    });

    await page.goto("/settings/profile");
    await expect(page.getByLabel("First name")).toHaveValue("Alice");

    // Someone else writes the profile while the user is typing.
    currentStamp = 2;

    await page.getByLabel("First name").fill("Alicia");
    await page.getByRole("button", { name: /save changes/i }).click();

    await expect(page.getByText(/profile changed elsewhere/i)).toBeVisible();
    expect(sentIfMatch[0]).toBe('"stamp-1"');
  });

  test("a 412 warns and keeps the edits instead of resending the stale body", async ({ page }) => {
    // Retrying the same body against a fresh tag performs exactly the overwrite the 412 just
    // prevented. The save stops, the user's typing stays on screen, and a deliberate second save
    // goes out against the version they were just told about.
    const sentIfMatch: string[] = [];
    let currentStamp = 1;

    await page.route("**/api/v1/identity/profile", async (route) => {
      const request = route.request();
      if (request.method() === "PUT") {
        const ifMatch = request.headers()["if-match"] ?? "";
        sentIfMatch.push(ifMatch);
        if (ifMatch !== `"stamp-${currentStamp}"`) {
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

      await route.fulfill({
        status: 200,
        headers: { ...ETAG_CORS_HEADERS, ETag: `"stamp-${currentStamp}"` },
        body: JSON.stringify(PROFILE),
      });
    });

    await page.goto("/settings/profile");
    await expect(page.getByLabel("First name")).toHaveValue("Alice");

    currentStamp = 2;
    await page.getByLabel("First name").fill("Alicia");
    await page.getByRole("button", { name: /save changes/i }).click();

    await expect(page.getByText(/profile changed elsewhere/i)).toBeVisible();
    await expect(page.getByText(/profile saved/i)).toBeHidden();
    // One PUT only: no silent retry behind the user's back.
    expect(sentIfMatch).toHaveLength(1);
    // The typing survived the rejection — nothing to retype.
    await expect(page.getByLabel("First name")).toHaveValue("Alicia");

    // Saving again now carries the version the warning told the user about.
    await page.getByRole("button", { name: /save changes/i }).click();
    await expect(page.getByText(/profile saved/i)).toBeVisible();
    expect(sentIfMatch).toHaveLength(2);
    expect(sentIfMatch[1]).toBe('"stamp-2"');
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
