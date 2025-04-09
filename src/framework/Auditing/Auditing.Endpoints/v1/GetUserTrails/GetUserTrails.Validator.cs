using FluentValidation;

namespace FSH.Framework.Auditing.Endpoints.v1.GetUserTrails;

public static partial class GetUserTrails
{
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).NotEmpty();
        }
    }
}
