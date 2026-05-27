import { apiFetch } from "@/lib/api-client";

export type TwoFactorEnrollmentResponse = {
  sharedKey: string;
  authenticatorUri: string;
};

const ROOT = "/api/v1/identity";

/**
 * Begin (or rotate) TOTP enrollment. The user has NOT yet enabled 2FA until
 * they confirm with a code via verifyEnrollTwoFactor — this just hands back
 * the secret + otpauth:// URI so the QR can render.
 */
export async function enrollTwoFactor(): Promise<TwoFactorEnrollmentResponse> {
  return apiFetch<TwoFactorEnrollmentResponse>(`${ROOT}/2fa/enroll`, { method: "POST" });
}

export async function verifyEnrollTwoFactor(code: string): Promise<{ success: boolean }> {
  return apiFetch<{ success: boolean }>(`${ROOT}/2fa/verify`, {
    method: "POST",
    body: JSON.stringify({ code }),
  });
}

export async function disableTwoFactor(currentPassword: string): Promise<{ success: boolean }> {
  return apiFetch<{ success: boolean }>(`${ROOT}/2fa/disable`, {
    method: "POST",
    body: JSON.stringify({ currentPassword }),
  });
}
