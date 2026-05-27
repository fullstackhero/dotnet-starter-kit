using FSH.Modules.Identity.Contracts.DTOs;
using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Impersonation.EndImpersonation;

public sealed record EndImpersonationCommand() : ICommand<TokenResponse>;
