import { apiFetch } from "@/lib/api-client";

export type SseTokenResponse = { token: string };

export function issueSseToken() {
  return apiFetch<SseTokenResponse>("/api/v1/sse/token", { method: "POST" });
}
