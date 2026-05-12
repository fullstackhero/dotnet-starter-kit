using FluentValidation;
using FSH.Modules.Chat.Contracts.v1.Commands;

namespace FSH.Modules.Chat.Features.v1.Channels.CreateChannel;

public sealed class CreateChannelCommandValidator : AbstractValidator<CreateChannelCommand>
{
    public CreateChannelCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
    }
}
