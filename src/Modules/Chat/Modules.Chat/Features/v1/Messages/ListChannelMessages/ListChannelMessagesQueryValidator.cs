using FluentValidation;
using FSH.Modules.Chat.Contracts.v1.Queries;

namespace FSH.Modules.Chat.Features.v1.Messages.ListChannelMessages;

public sealed class ListChannelMessagesQueryValidator : AbstractValidator<ListChannelMessagesQuery>
{
    public ListChannelMessagesQueryValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
