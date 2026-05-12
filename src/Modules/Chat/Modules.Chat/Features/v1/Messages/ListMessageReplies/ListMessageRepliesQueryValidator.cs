using FluentValidation;
using FSH.Modules.Chat.Contracts.v1.Queries;

namespace FSH.Modules.Chat.Features.v1.Messages.ListMessageReplies;

public sealed class ListMessageRepliesQueryValidator : AbstractValidator<ListMessageRepliesQuery>
{
    public ListMessageRepliesQueryValidator()
    {
        RuleFor(x => x.ParentMessageId).NotEmpty();
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
