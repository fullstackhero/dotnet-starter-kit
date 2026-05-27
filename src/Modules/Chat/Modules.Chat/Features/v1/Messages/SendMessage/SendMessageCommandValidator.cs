using FluentValidation;
using FSH.Modules.Chat.Contracts.v1.Commands;

namespace FSH.Modules.Chat.Features.v1.Messages.SendMessage;

public sealed class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
        // Body is optional when at least one attachment is present (Slack /
        // Teams parity — users frequently send "here's the file" with no
        // accompanying text). Length cap still applies whenever body is
        // populated.
        RuleFor(x => x.Body)
            .NotEmpty()
            .When(x => x.Attachments is null || x.Attachments.Count == 0)
            .WithMessage("Either a body or an attachment is required.");
        RuleFor(x => x.Body)
            .MaximumLength(32_768)
            .When(x => !string.IsNullOrEmpty(x.Body));
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
