using FSH.Modules.Files.Contracts.v1.DTOs;
using Mediator;

namespace FSH.Modules.Files.Contracts.v1.Queries;

public sealed record GetFileDownloadUrlQuery(Guid FileAssetId) : IQuery<PresignedDownloadResponse>;
