using FluentValidation;
using FSH.Modules.Chat.Contracts.v1.Commands;

namespace FSH.Modules.Chat.Features.v1.Messages.SendMessage;

public sealed class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
        RuleFor(x => x.Body).NotEmpty().MaximumLength(32_768);
        RuleFor(x => x.Attachments).NotNull();
        RuleFor(x => x.Attachments.Count).LessThanOrEqualTo(10)
            .When(x => x.Attachments is not null);
        RuleForEach(x => x.Attachments).ChildRules(att =>
        {
            att.RuleFor(a => a.Url).NotEmpty().MaximumLength(2048);
            att.RuleFor(a => a.ContentType).NotEmpty().MaximumLength(255);
            att.RuleFor(a => a.FileName).NotEmpty().MaximumLength(512);
            att.RuleFor(a => a.SizeBytes).GreaterThanOrEqualTo(0);
        });
    }
}
