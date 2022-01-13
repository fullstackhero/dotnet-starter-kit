namespace FSH.WebApi.Application.Multitenancy;

public class CreateTenantRequestValidator : CustomValidator<CreateTenantRequest>
{
    public CreateTenantRequestValidator(
        ITenantReadRepository repository,
        IStringLocalizer<CreateTenantRequestValidator> localizer,
        ITenantDatabaseService tenantDbService)
    {
        RuleFor(t => t.Key).Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (key, ct) => await repository.GetBySpecAsync(new TenantByKeySpec(key!), ct) is null)
            .WithMessage((_, key) => string.Format(localizer["tenant.alreadyexists"], key));

        RuleFor(t => t.ConnectionString).Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must((t, cs) => string.IsNullOrWhiteSpace(cs) || tenantDbService.TryValidateConnectionString(cs, t.Key))
            .WithMessage(localizer["invalid.connectionstring"]);

        RuleFor(t => t.Name).Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (name, ct) => await repository.GetBySpecAsync(new TenantByNameSpec(name!), ct) is null)
            .WithMessage((_, name) => string.Format(localizer["tenant.alreadyexists"], name));

        RuleFor(t => t.AdminEmail).Cascade(CascadeMode.Stop)
            .NotEmpty().EmailAddress();
    }
}