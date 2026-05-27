using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Sessions.RevokeSession;

public sealed record RevokeSessionCommand(Guid SessionId) : ICommand<bool>;