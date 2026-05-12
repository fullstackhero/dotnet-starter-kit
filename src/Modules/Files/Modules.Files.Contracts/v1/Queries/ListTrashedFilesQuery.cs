using System.Collections.ObjectModel;
using FSH.Modules.Files.Contracts.v1.DTOs;
using Mediator;

namespace FSH.Modules.Files.Contracts.v1.Queries;

public sealed record ListTrashedFilesQuery(int Page = 1, int PageSize = 50) : IQuery<ReadOnlyCollection<FileAssetDto>>;
