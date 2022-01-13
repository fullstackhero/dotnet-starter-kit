namespace FSH.WebApi.Application.Multitenancy;

public class CreateTenantRequestValidator : CustomValidator<CreateTenantRequest>
{
    public CreateTenantRequestValidator(
        ITenantService tenantService,
        IStringLocalizer<CreateTenantRequestValidator> localizer,
        IConnectionStringValidator connectionStringValidator)
    {
        RuleFor(t => t.TenantId).Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (id, _) => !await tenantService.ExistsWithIdAsync(id))
                .WithMessage((_, id) => string.Format(localizer["tenant.alreadyexists"], id));

        RuleFor(t => t.Name).Cascade(CascadeMode.Stop)
            .NotEmpty();

        RuleFor(t => t.ConnectionString)
            .Must((_, cs) => string.IsNullOrWhiteSpace(cs) || connectionStringValidator.TryValidate(cs))
                .WithMessage(localizer["invalid.connectionstring"]);

        RuleFor(t => t.AdminEmail).Cascade(CascadeMode.Stop)
            .NotEmpty()
            .EmailAddress();
    }
}