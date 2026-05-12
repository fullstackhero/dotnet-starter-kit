using Mediator;

namespace FSH.Modules.Files.Contracts.v1.Commands;

public sealed record DeleteFileCommand(Guid FileAssetId) : ICommand<Unit>;
