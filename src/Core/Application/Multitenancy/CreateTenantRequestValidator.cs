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

        RuleFor(t => t.ConnectionString)
            .Must((t, cs) => string.IsNullOrWhiteSpace(cs) || tenantDbService.TryValidateConnectionString(cs, t.Key))
                .WithMessage(localizer["invalid.connectionstring"]);
    }
}