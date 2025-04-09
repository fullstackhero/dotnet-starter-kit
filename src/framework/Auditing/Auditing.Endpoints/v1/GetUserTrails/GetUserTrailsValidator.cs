namespace FSH.Framework.Auditing.Endpoints.v1.GetUserTrails;
using FluentValidation;

public sealed class GetUserTrailsValidator : AbstractValidator<GetUserTrailsQuery>
{
    public GetUserTrailsValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
