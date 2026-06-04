import { apiFetch } from "@/lib/api-client";
import type { PagedResponse } from "@/api/catalog";

// Mirrors FSH.Modules.Files.Contracts.v1.DTOs.Visibility. The API serializes enums as
// their string name (global JsonStringEnumConverter).
export const Visibility = {
  Public: "Public",
  Private: "Private",
} as const;
export type VisibilityValue = (typeof Visibility)[keyof typeof Visibility];

// Mirrors FSH.Modules.Files.Domain.FileAssetStatus.
export const FileAssetStatus = {
  PendingUpload: "PendingUpload",
  Available: "Available",
  Quarantined: "Quarantined",
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
  /** The user that uploaded the file. Use with useUserDisplay to resolve a name.
   *  Older server versions before the field was added send "" — guard against it
   *  when deciding whether to render an "uploaded by" attribution row. */
  createdByUserId: string;
  deletedOnUtc?: string | null;
  deletedBy?: string | null;
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

export function listSharedFiles(page = 1, pageSize = 20): Promise<FileAssetDto[]> {
  return apiFetch<FileAssetDto[]>(
    `/api/v1/files/shared?page=${page}&pageSize=${pageSize}`,
  );
}

/** Flip a file's visibility. Server returns the refreshed DTO so the client can patch
 *  its preview/list without a follow-up GET. */
export function changeFileVisibility(
  fileAssetId: string,
  visibility: VisibilityValue,
): Promise<FileAssetDto> {
  return apiFetch<FileAssetDto>(
    `/api/v1/files/${encodeURIComponent(fileAssetId)}/visibility`,
    {
      method: "PATCH",
      body: JSON.stringify({ visibility }),
    },
  );
}

export function deleteFile(fileAssetId: string): Promise<void> {
  return apiFetch<void>(`/api/v1/files/${encodeURIComponent(fileAssetId)}`, {
    method: "DELETE",
  });
}

export function listTrashedFiles(
  pageNumber = 1,
  pageSize = 20,
): Promise<PagedResponse<FileAssetDto>> {
  return apiFetch<PagedResponse<FileAssetDto>>(
    `/api/v1/files/trash?pageNumber=${pageNumber}&pageSize=${pageSize}`,
  );
}

export function restoreFile(fileAssetId: string): Promise<void> {
  return apiFetch<void>(
    `/api/v1/files/${encodeURIComponent(fileAssetId)}/restore`,
    { method: "POST" },
  );
}
