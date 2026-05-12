using FluentValidation;
using FSH.Modules.Chat.Contracts.v1.Commands;

namespace FSH.Modules.Chat.Features.v1.Channels.FindOrCreateDm;

public sealed class FindOrCreateDmCommandValidator : AbstractValidator<FindOrCreateDmCommand>
{
    public FindOrCreateDmCommandValidator()
    {
        // UserIds contains the OTHER participants; 1 = DM, 2..8 = group DM (cap at 9 total members).
        RuleFor(x => x.UserIds).NotNull().NotEmpty();
        RuleFor(x => x.UserIds.Count).InclusiveBetween(1, 8).When(x => x.UserIds is not null);
        RuleForEach(x => x.UserIds).NotEmpty().MaximumLength(64);
    }
}
