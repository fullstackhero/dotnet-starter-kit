// E2E coverage for the /files page (src/pages/files/my-files.tsx): the file
// list, the My files / Shared tab switch, the empty states, and the upload
// dropzone affordance. All API calls are mocked via page.route; the authed
// session is seeded into localStorage and the global shell calls are stubbed
// by installShellMocks. Browser: chromium only, run against the already-
// running Vite dev server.
//
// Gotcha note: the list renders BOTH a hidden mobile card and a desktop row,
// so the same filename matches twice. Row assertions therefore use .last()
// to avoid strict-mode hits. The page fires GET /files/mine on load and GET
// /files/shared only once the Shared tab is selected — mock both.

import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks } from "../helpers/shell-mocks";

// ─── Fixtures ────────────────────────────────────────────────────────────

const FILE_REPORT = {
  id: "00000000-0000-0000-0000-0000000f1111",
  ownerType: "MyFiles",
  ownerId: null,
  originalFileName: "quarterly-report.pdf",
  contentType: "application/pdf",
  sizeBytes: 248_000,
  visibility: "Private",
  status: "Available",
  scanStatus: 1,
  createdAtUtc: "2026-05-10T10:00:00Z",
  publicUrl: null,
  createdByUserId: TEST_USER.sub,
};

const FILE_SHARED = {
  id: "00000000-0000-0000-0000-0000000f2222",
  ownerType: "MyFiles",
  ownerId: null,
  originalFileName: "team-handbook.pdf",
  contentType: "application/pdf",
  sizeBytes: 512_000,
  visibility: "Public",
  status: "Available",
  scanStatus: 1,
  createdAtUtc: "2026-05-11T10:00:00Z",
  publicUrl: "https://cdn.example.com/team-handbook.pdf",
  createdByUserId: "u-other-1",
};

// ─── Shared beforeEach ──────────────────────────────────────────────────

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, TEST_USER);
  await installShellMocks(page);
  // The shared-file "uploaded by" attribution resolves uploader names via
  // GET /identity/users/{id}. Mock it so those rows don't 404-retry.
  await mockJsonResponse(page, "**/api/v1/identity/users/**", {
    id: "u-other-1",
    userName: "bob",
    email: "bob@acme.com",
    firstName: "Bob",
    lastName: "Stone",
  });
});

// ─── List render ──────────────────────────────────────────────────────

test.describe("files — list render", () => {
  test("renders the Files heading and a file row from mocked data", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/files/mine**", [FILE_REPORT]);

    await page.goto("/files");

    await expect(page.getByRole("heading", { name: "Files", level: 1 })).toBeVisible();
    // Both the mobile card and desktop row print the name — scope to the
    // last (desktop table) match so the assertion is deterministic.
    await expect(page.getByText("quarterly-report.pdf").last()).toBeVisible();
    // The "N file(s)" count prints twice (page-header total + list count) —
    // assert the first match so strict mode stays satisfied.
    await expect(page.getByText(/1 file$/).first()).toBeVisible();
  });

  test("shows the upload dropzone affordance by its accessible name", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/files/mine**", [FILE_REPORT]);

    await page.goto("/files");

    // The dropzone is a role="button" carrying its drop-or-browse caption.
    await expect(
      page.getByRole("button", { name: /drop a file or click to browse/i }),
    ).toBeVisible();
  });

  test("shows the empty state when the tenant has no files", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/files/mine**", []);

    await page.goto("/files");

    await expect(page.getByRole("heading", { name: /no files yet/i })).toBeVisible();
    await expect(
      page.getByText(/drop a file above to get started/i),
    ).toBeVisible();
  });
});

// ─── Tab switching ────────────────────────────────────────────────────

test.describe("files — My files / Shared tabs", () => {
  test("switching to Shared loads shared files and renders a shared row", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/files/mine**", [FILE_REPORT]);
    await mockJsonResponse(page, "**/api/v1/files/shared**", [FILE_SHARED]);

    await page.goto("/files");

    // My files row is visible first.
    await expect(page.getByText("quarterly-report.pdf").last()).toBeVisible();

    await page.getByRole("tab", { name: /shared in tenant/i }).click();

    await expect(page.getByText("team-handbook.pdf").last()).toBeVisible();
    // The shared tab is now selected.
    await expect(
      page.getByRole("tab", { name: /shared in tenant/i }),
    ).toHaveAttribute("aria-selected", "true");
  });

  test("Shared tab shows its own empty state when nothing is shared", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/files/mine**", [FILE_REPORT]);
    await mockJsonResponse(page, "**/api/v1/files/shared**", []);

    await page.goto("/files");
    await page.getByRole("tab", { name: /shared in tenant/i }).click();

    await expect(page.getByRole("heading", { name: /nothing shared yet/i })).toBeVisible();
    await expect(
      page.getByText(/when a teammate flips one of their files to public/i),
    ).toBeVisible();
  });
});
