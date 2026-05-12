using Mediator;

namespace FSH.Modules.Chat.Contracts.v1.Commands;

/// <summary>
/// Find or create a DM/group DM. <paramref name="UserIds"/> contains the OTHER participants;
/// the current user is added implicitly. Single-other = DirectMessage (deterministic lookup via
/// DirectKey). Multi = GroupMessage (always created fresh).
/// </summary>
public sealed record FindOrCreateDmCommand(IReadOnlyList<string> UserIds) : ICommand<Guid>;
