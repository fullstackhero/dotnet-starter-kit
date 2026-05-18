import { apiFetch } from "@/lib/api-client";
import type { PagedResponse } from "@/lib/api-types";

export type WebhookSubscriptionDto = {
  id: string;
  url: string;
  events: string[];
  isActive: boolean;
  createdAtUtc: string;
};

export type WebhookDeliveryDto = {
  id: string;
  subscriptionId: string;
  eventType: string;
  httpStatusCode: number;
  success: boolean;
  attemptCount: number;
  attemptedAtUtc: string;
  errorMessage?: string | null;
};

export type CreateWebhookSubscriptionInput = {
  url: string;
  events: string[];
  secret?: string;
};

const ROOT = "/api/v1/webhooks";

export function listWebhookSubscriptions(
  pageNumber = 1,
  pageSize = 50,
): Promise<PagedResponse<WebhookSubscriptionDto>> {
  const q = new URLSearchParams({
    pageNumber: String(pageNumber),
    pageSize: String(pageSize),
  });
  return apiFetch<PagedResponse<WebhookSubscriptionDto>>(`${ROOT}/subscriptions?${q.toString()}`);
}

export function createWebhookSubscription(input: CreateWebhookSubscriptionInput): Promise<string> {
  return apiFetch<string>(`${ROOT}/subscriptions`, {
    method: "POST",
    body: JSON.stringify({
      url: input.url,
      events: input.events,
      secret: input.secret?.trim() ? input.secret : null,
    }),
  });
}

export function deleteWebhookSubscription(id: string): Promise<void> {
  return apiFetch<void>(`${ROOT}/subscriptions/${encodeURIComponent(id)}`, {
    method: "DELETE",
  });
}

export function testWebhookSubscription(id: string): Promise<{ success: boolean }> {
  return apiFetch<{ success: boolean }>(
    `${ROOT}/subscriptions/${encodeURIComponent(id)}/test`,
    { method: "POST" },
  );
}

export function listWebhookDeliveries(
  subscriptionId: string,
  pageNumber = 1,
  pageSize = 50,
): Promise<PagedResponse<WebhookDeliveryDto>> {
  const q = new URLSearchParams({
    pageNumber: String(pageNumber),
    pageSize: String(pageSize),
  });
  return apiFetch<PagedResponse<WebhookDeliveryDto>>(
    `${ROOT}/subscriptions/${encodeURIComponent(subscriptionId)}/deliveries?${q.toString()}`,
  );
}

/**
 * Curated list of event names commonly emitted by FSH modules. Webhook
 * subscriptions accept arbitrary strings — these just power the chip
 * picker in the create dialog so operators don't have to remember the
 * canonical kebab-case names.
 */
export const SUGGESTED_EVENT_TYPES: readonly string[] = [
  "tenant.created",
  "tenant.activation.changed",
  "user.registered",
  "user.role.assigned",
  "billing.invoice.issued",
  "billing.invoice.paid",
  "billing.subscription.created",
  "billing.subscription.cancelled",
];
