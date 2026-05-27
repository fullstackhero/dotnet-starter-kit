using System.Collections.ObjectModel;
using FSH.Modules.Files.Contracts.v1.DTOs;
using Mediator;

namespace FSH.Modules.Files.Contracts.v1.Queries;

/// <summary>
/// List files marked Public and tagged with the built-in tenant-wide owner types
/// (<c>MyFiles</c>, <c>User</c>) — the inverse of <c>ListMyFilesQuery</c>. Domain-bound
/// attachments (Product images, Ticket files, Chat messages) deliberately don't show up
/// here: their visibility is a function of their owning entity's access rules, not a
/// free-standing share decision.
/// </summary>
public sealed record ListSharedFilesQuery(int Page = 1, int PageSize = 20)
    : IQuery<ReadOnlyCollection<FileAssetDto>>;
