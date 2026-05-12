import { apiFetch } from "@/lib/api-client";

// Mirrors FSH.Modules.Files.Domain.Visibility — Public/Private numeric codes
// match the server's int? Visibility shape on the FileAssetDto.
export const Visibility = {
  Public: 0,
  Private: 1,
} as const;
export type VisibilityValue = (typeof Visibility)[keyof typeof Visibility];

// Mirrors FSH.Modules.Files.Domain.FileAssetStatus.
export const FileAssetStatus = {
  PendingUpload: 0,
  Available: 1,
  Quarantined: 2,
} as const;
export type FileAssetStatusValue = (typeof FileAssetStatus)[keyof typeof FileAssetStatus];

// Mirrors FSH.Modules.Files.Contracts.v1.DTOs.FileAssetDto.
export type FileAssetDto = {
  id: string;
  ownerType: string;
  ownerId?: string | null;
  originalFileName: string;
  contentType: string;
  sizeBytes: number;
  visibility: VisibilityValue;
  status: FileAssetStatusValue;
  scanStatus: number;
  createdAtUtc: string;
  publicUrl?: string | null;
};

export type PresignedUploadResponse = {
  fileAssetId: string;
  uploadUrl: string;
  requiredHeaders: Record<string, string>;
  expiresAt: string;
};

export type PresignedDownloadResponse = {
  url: string;
  expiresAt: string;
};

export type RequestUploadUrlInput = {
  ownerType: string;
  ownerId?: string | null;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  visibility: VisibilityValue;
  category: string;
};

export function requestUploadUrl(input: RequestUploadUrlInput): Promise<PresignedUploadResponse> {
  return apiFetch<PresignedUploadResponse>("/api/v1/files/upload-url", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export function finalizeUpload(fileAssetId: string): Promise<FileAssetDto> {
  return apiFetch<FileAssetDto>(
    `/api/v1/files/${encodeURIComponent(fileAssetId)}/finalize`,
    { method: "POST" },
  );
}

export function getFileMetadata(fileAssetId: string): Promise<FileAssetDto> {
  return apiFetch<FileAssetDto>(`/api/v1/files/${encodeURIComponent(fileAssetId)}`);
}

export function getFileDownloadUrl(
  fileAssetId: string,
  options: { inline?: boolean } = {},
): Promise<PresignedDownloadResponse> {
  const qs = options.inline ? "?inline=true" : "";
  return apiFetch<PresignedDownloadResponse>(
    `/api/v1/files/${encodeURIComponent(fileAssetId)}/url${qs}`,
  );
}

export function listMyFiles(page = 1, pageSize = 20): Promise<FileAssetDto[]> {
  return apiFetch<FileAssetDto[]>(
    `/api/v1/files/mine?page=${page}&pageSize=${pageSize}`,
  );
}

export function deleteFile(fileAssetId: string): Promise<void> {
  return apiFetch<void>(`/api/v1/files/${encodeURIComponent(fileAssetId)}`, {
    method: "DELETE",
  });
}

export function listTrashedFiles(page = 1, pageSize = 50): Promise<FileAssetDto[]> {
  return apiFetch<FileAssetDto[]>(
    `/api/v1/files/trash?page=${page}&pageSize=${pageSize}`,
  );
}

export function restoreFile(fileAssetId: string): Promise<void> {
  return apiFetch<void>(
    `/api/v1/files/${encodeURIComponent(fileAssetId)}/restore`,
    { method: "POST" },
  );
}
