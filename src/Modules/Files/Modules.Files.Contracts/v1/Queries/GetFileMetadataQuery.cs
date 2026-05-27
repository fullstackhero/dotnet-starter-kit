using FSH.Modules.Files.Contracts.v1.DTOs;
using Mediator;

namespace FSH.Modules.Files.Contracts.v1.Queries;

public sealed record GetFileMetadataQuery(Guid FileAssetId) : IQuery<FileAssetDto>;
