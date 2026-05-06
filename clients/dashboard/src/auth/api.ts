import { apiFetch } from "@/lib/api-client";

export type TokenResponse = {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
};

export function issueToken(input: {
  email: string;
  password: string;
  tenant: string;
}) {
  return apiFetch<TokenResponse>("/api/v1/identity/token/issue", {
    method: "POST",
    body: JSON.stringify({ email: input.email, password: input.password }),
    // X-FSH-App tells the API this credential request originated from the
    // tenant dashboard. The server uses it to enforce the SuperAdmin / app
    // boundary — a root-tenant login submitted with X-FSH-App=dashboard is
    // rejected with 403 instead of receiving a usable token.
    headers: { tenant: input.tenant, "X-FSH-App": "dashboard" },
    skipAuth: true,
  });
}
