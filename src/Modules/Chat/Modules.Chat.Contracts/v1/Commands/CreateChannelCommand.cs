using Mediator;

namespace FSH.Modules.Chat.Contracts.v1.Commands;

/// <summary>
/// Create a named channel. Creator becomes the first member with Admin role. Use <see cref="FindOrCreateDmCommand"/>
/// for DMs / group DMs instead.
/// </summary>
public sealed record CreateChannelCommand(
    string Name,
    string? Description,
    bool IsPrivate) : ICommand<Guid>;
