using FSH.Modules.Files.Contracts.v1.DTOs;
using Mediator;

namespace FSH.Modules.Files.Contracts.v1.Queries;

/// <summary>
/// Mint a short-lived presigned GET URL for a FileAsset. When <paramref name="Inline"/> is
/// <c>true</c>, the URL asks S3/MinIO to send <c>Content-Disposition: inline</c> so the
/// browser renders the file in place (PDF viewer, image, etc.) instead of downloading it.
/// Default is <c>false</c> (attachment) — the click-to-save behavior.
/// </summary>
public sealed record GetFileDownloadUrlQuery(Guid FileAssetId, bool Inline = false) : IQuery<PresignedDownloadResponse>;
