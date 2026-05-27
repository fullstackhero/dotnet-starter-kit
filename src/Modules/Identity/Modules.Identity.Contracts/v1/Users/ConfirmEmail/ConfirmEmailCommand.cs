using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Users.ConfirmEmail;

public sealed record ConfirmEmailCommand(string UserId, string Code, string Tenant) : ICommand<string>;