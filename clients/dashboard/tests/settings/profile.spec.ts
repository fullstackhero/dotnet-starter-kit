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

  // A compressing edge re-encodes the response and downgrades the validator it forwards:
  // Cloudflare does exactly this by default once Brotli/gzip is on. The endpoint only ever emits a
  // strong tag, so a weak one reaching the client is a transport artefact — and echoing it back
  // unchanged means the server drops it under the strong comparison If-Match mandates and answers
  // 412 to every save, forever, on a profile nobody else is touching.
  test("sends a strong If-Match even when the edge downgraded the ETag to a weak one", async ({
    page,
  }) => {
    await page.route("**/api/v1/identity/profile", async (route) => {
      if (route.request().method() === "PUT") {
        await route.fulfill({ status: 200, headers: { "Content-Type": "application/json" }, body: '""' });
        return;
      }
      await route.fulfill({
        status: 200,
        headers: { ...ETAG_CORS_HEADERS, ETag: 'W/"stamp-1"' },
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

    expect(putReq.headers()["if-match"]).toBe('"stamp-1"');
  });

  // Setting the image is a second write to the same row, so Identity rotates the concurrency stamp.
  // Without adopting the new version the next save carries the pre-image tag, gets a 412, and the
  // user is told someone else edited their profile — on a profile only they touched.
  test("a save after changing the avatar carries the tag the image write produced", async ({ page }) => {
    let currentStamp = 1;
    const sentIfMatch: string[] = [];

    await page.route("**/api/v1/identity/profile/image", async (route) => {
      currentStamp += 1;
      await route.fulfill({ status: 200, headers: { "Content-Type": "application/json" }, body: '""' });
    });

    await page.route("**/api/v1/identity/profile", async (route) => {
      const request = route.request();
      if (request.method() === "PUT") {
        sentIfMatch.push(request.headers()["if-match"] ?? "");
        await route.fulfill({ status: 200, headers: { "Content-Type": "application/json" }, body: '""' });
        return;
      }
      await route.fulfill({
        status: 200,
        headers: { ...ETAG_CORS_HEADERS, ETag: `"stamp-${currentStamp}"` },
        body: JSON.stringify({ ...PROFILE, imageUrl: "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAAC0lEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==" }),
      });
    });

    await page.goto("/settings/profile");
    await expect(page.getByLabel("First name")).toHaveValue("Alice");

    await page.getByRole("button", { name: /^remove$/i }).click();
    await expect(page.getByText(/profile image updated/i)).toBeVisible();

    await page.getByLabel("First name").fill("Alicia");
    await page.getByRole("button", { name: /save changes/i }).click();

    await expect.poll(() => sentIfMatch).toEqual(['"stamp-2"']);
  });

  // Without the profile read there is no ETag and no unedited-field values, so a save would either
  // be a silent no-op or blank the fields it cannot see. The button is disabled and says why —
  // which nothing exercised, so re-enabling it would not have failed anything.
  test("saving is disabled while the profile read is failing", async ({ page }) => {
    await page.route("**/api/v1/identity/profile", async (route) => {
      if (route.request().method() === "GET") {
        await route.fulfill({
          status: 500,
          headers: { "Content-Type": "application/problem+json" },
          body: JSON.stringify({ status: 500, title: "Server Error" }),
        });
        return;
      }
      throw new Error("no write may be attempted while the read is failing");
    });

    await page.goto("/settings/profile");

    await expect(page.getByText(/saving is disabled/i)).toBeVisible();
    await expect(page.getByRole("button", { name: /save changes/i })).toBeDisabled();
  });
});
