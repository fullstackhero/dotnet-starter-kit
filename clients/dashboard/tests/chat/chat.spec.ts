// E2E coverage for the /chat shell (src/pages/chat/chat-page.tsx): the
// channel rail, the active-channel pane (header + message list + composer),
// and the no-channels empty state. All API calls are mocked via page.route;
// the authed session is seeded into localStorage and the global shell calls
// are stubbed by installShellMocks.
//
// Chat is realtime-heavy. installShellMocks aborts the SSE / SignalR /
// realtime transports, so the connection never establishes — the page must
// still render from REST alone. We assert render + presence (channels show,
// composer present, message body renders) rather than deep realtime
// behaviour, which is too flaky to drive deterministically here.
//
// ORDERING: installShellMocks stubs **/api/v1/chat/channels** → [] for the
// topbar unread badge. We register our channel mocks AFTER it so they win
// (Playwright matches the most-recently-registered route first). Within our
// own mocks the bare channels list is registered first and the more specific
// by-id + messages routes last so they take precedence over the list glob.
//
// Gotcha: opening /chat with channels present auto-navigates to
// /chat/{firstChannelId} (replace), which then fires getChannelById +
// listChannelMessages. Mock every load endpoint or the pane hangs.

import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks } from "../helpers/shell-mocks";

// ─── Fixtures ────────────────────────────────────────────────────────────

const CHANNEL_ID = "00000000-0000-0000-0000-0000000c1111";

// type "Channel" (named, public). Using a named channel keeps the header /
// composer title deterministic — channelTitle returns channel.name verbatim
// (no Identity lookup needed, unlike DMs which resolve a partner name).
const CHANNEL_ENGINEERING = {
  id: CHANNEL_ID,
  type: "Channel",
  name: "engineering",
  slug: "engineering",
  description: "Where the builders talk.",
  isPrivate: false,
  createdByUserId: TEST_USER.sub,
  createdAtUtc: "2026-05-01T10:00:00Z",
  updatedAtUtc: null,
  lastMessageAtUtc: "2026-05-12T10:00:00Z",
  unreadCount: 0,
  members: [
    {
      id: "m-1",
      userId: TEST_USER.sub,
      role: "Admin",
      joinedAtUtc: "2026-05-01T10:00:00Z",
      lastReadMessageId: null,
    },
  ],
};

const MESSAGE_HELLO = {
  id: "00000000-0000-0000-0000-0000000d2222",
  channelId: CHANNEL_ID,
  authorUserId: TEST_USER.sub,
  body: "Ship it when the tests are green.",
  parentMessageId: null,
  replyCount: 0,
  editedAtUtc: null,
  deletedAtUtc: null,
  createdAtUtc: "2026-05-12T10:00:00Z",
  attachments: [],
  reactions: [],
};

/**
 * Wire the channel-list, channel-by-id, and messages reads. Register the bare
 * list first, then the more specific by-id + messages routes (they win over
 * the list glob, and over the shell's [] stub since they're registered later).
 * Also stubs identity/users so message-author name resolution doesn't 404.
 */
async function mockChannelWithMessages(
  page: import("@playwright/test").Page,
  messages: typeof MESSAGE_HELLO[],
) {
  await mockJsonResponse(page, "**/api/v1/identity/users/**", {
    id: TEST_USER.sub,
    userName: "alice",
    email: TEST_USER.email,
    firstName: TEST_USER.firstName,
    lastName: TEST_USER.lastName,
  });
  await mockJsonResponse(page, "**/api/v1/chat/channels?**", [CHANNEL_ENGINEERING]);
  await mockJsonResponse(page, "**/api/v1/chat/channels", [CHANNEL_ENGINEERING]);
  await mockJsonResponse(page, `**/api/v1/chat/channels/${CHANNEL_ID}/messages**`, messages);
  await mockJsonResponse(page, `**/api/v1/chat/channels/${CHANNEL_ID}/pinned`, []);
  await mockJsonResponse(page, `**/api/v1/chat/channels/${CHANNEL_ID}/read`, '""');
  await mockJsonResponse(page, `**/api/v1/chat/channels/${CHANNEL_ID}`, CHANNEL_ENGINEERING);
}

// ─── Shared beforeEach ──────────────────────────────────────────────────

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, TEST_USER);
  await installShellMocks(page);
});

// ─── Channel rail render ──────────────────────────────────────────────

test.describe("chat — channel rail", () => {
  test("shows the Chat heading and a channel name from the mocked list", async ({ page }) => {
    await mockChannelWithMessages(page, [MESSAGE_HELLO]);

    await page.goto("/chat");

    // The rail's brand mark.
    await expect(page.getByText("Chat", { exact: true })).toBeVisible();
    // The channel row label. The rail and (after auto-select) the header both
    // print "engineering" — assert at least one is visible.
    await expect(page.getByText("engineering").first()).toBeVisible();
  });
});

// ─── Active channel pane ──────────────────────────────────────────────

test.describe("chat — active channel pane", () => {
  test("auto-selects the first channel and renders its message body", async ({ page }) => {
    await mockChannelWithMessages(page, [MESSAGE_HELLO]);

    // Deep-link straight to the channel so we don't depend on the auto-nav
    // redirect timing.
    await page.goto(`/chat/${CHANNEL_ID}`);

    await expect(page.getByText(/ship it when the tests are green/i)).toBeVisible({
      timeout: 8_000,
    });
  });

  test("renders the composer with its channel-scoped placeholder", async ({ page }) => {
    await mockChannelWithMessages(page, [MESSAGE_HELLO]);

    await page.goto(`/chat/${CHANNEL_ID}`);

    // Named channels (type 2) get the "Message channel {name}" aria-label and
    // a "Message #{name}" placeholder. Assert the composer textarea is present.
    const composer = page.getByRole("textbox", { name: /message channel engineering/i });
    await expect(composer).toBeVisible({ timeout: 8_000 });
    await expect(page.getByRole("button", { name: /send message/i })).toBeVisible();
  });

  test("shows the no-messages state for an empty channel but still renders the composer", async ({ page }) => {
    await mockChannelWithMessages(page, []);

    await page.goto(`/chat/${CHANNEL_ID}`);

    await expect(page.getByText(/no messages yet/i)).toBeVisible({ timeout: 8_000 });
    await expect(
      page.getByRole("textbox", { name: /message channel engineering/i }),
    ).toBeVisible();
  });
});

// ─── Empty state ──────────────────────────────────────────────────────

test.describe("chat — empty state", () => {
  test("shows the 'Pick a conversation' empty state when there are no channels", async ({ page }) => {
    // Override the shell's [] explicitly for clarity; with no channels the
    // page does NOT auto-navigate, so the EmptyState renders.
    await mockJsonResponse(page, "**/api/v1/chat/channels?**", []);
    await mockJsonResponse(page, "**/api/v1/chat/channels", []);

    await page.goto("/chat");

    await expect(
      page.getByRole("heading", { name: /pick a conversation/i }),
    ).toBeVisible();
    // The rail's own empty hint.
    await expect(page.getByText(/no channels yet/i)).toBeVisible();
  });
});
