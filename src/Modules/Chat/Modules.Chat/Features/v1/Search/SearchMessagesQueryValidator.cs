using FluentValidation;
using FSH.Modules.Chat.Contracts.v1.Queries;

namespace FSH.Modules.Chat.Features.v1.Search;

public sealed class SearchMessagesQueryValidator : AbstractValidator<SearchMessagesQuery>
{
    public SearchMessagesQueryValidator()
    {
        RuleFor(x => x.Q).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
