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
    // X-FSH-App marks this client as the platform-admin app. Used by the
    // API to enforce the SuperAdmin / dashboard boundary.
    headers: { tenant: input.tenant, "X-FSH-App": "admin" },
    skipAuth: true,
  });
}
