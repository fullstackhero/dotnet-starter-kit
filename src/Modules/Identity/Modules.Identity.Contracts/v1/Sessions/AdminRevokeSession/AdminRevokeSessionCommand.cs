using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Sessions.AdminRevokeSession;

public sealed record AdminRevokeSessionCommand(Guid UserId, Guid SessionId, string? Reason = null) : ICommand<bool>;