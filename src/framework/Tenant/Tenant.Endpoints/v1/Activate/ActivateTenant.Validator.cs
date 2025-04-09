using FluentValidation;

namespace FSH.Framework.Tenant.Endpoints.v1.Activate;
public static partial class ActivateTenant
{
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator() =>
           RuleFor(t => t.TenantId)
               .NotEmpty();
    }

}
