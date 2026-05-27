using Mediator;

namespace FSH.Modules.Files.Contracts.v1.Commands;

public sealed record RestoreFileCommand(Guid FileAssetId) : ICommand<Unit>;
