using FluentValidation;
using FSH.Framework.Auditing.Contracts.v1.GetUserTrails;

namespace FSH.Framework.Auditing.Features.v1.GetUserTrails;
internal sealed class GetUserTrailsQueryValidator : AbstractValidator<GetUserTrailsQuery>
{
    public GetUserTrailsQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}