namespace FSH.WebApi.Application.Multitenancy;

public class CreateTenantRequestValidator : CustomValidator<CreateTenantRequest>
{
    public CreateTenantRequestValidator(
        ITenantService tenantService,
        IStringLocalizer<CreateTenantRequestValidator> T,
        IConnectionStringValidator connectionStringValidator)
    {
        RuleFor(t => t.Id).Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (id, _) => !await tenantService.ExistsWithIdAsync(id))
                .WithMessage((_, id) => T["Tenant {0} already exists.", id]);

        RuleFor(t => t.Name).Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (name, _) => !await tenantService.ExistsWithNameAsync(name!))
                .WithMessage((_, name) => T["Tenant {0} already exists.", name]);

        RuleFor(t => t.DbProvider).Cascade(CascadeMode.Stop)
            .Must(dbProvider => dbProvider == null || new[] { "postgresql", "mssql", "mysql", "oracle", "sqlite" }.Contains(dbProvider.ToLowerInvariant()))
            .WithMessage("DbProvider must be one of the following: postgresql, mssql, mysql, oracle, sqlite");

        RuleFor(t => t.ConnectionString).Cascade(CascadeMode.Stop)
            .Must((request, connectionString) => request.DbProvider == null || connectionString == null || connectionStringValidator.TryValidate(connectionString, request.DbProvider))
            .WithMessage("Invalid ConnectionString for the provided DbProvider");

        RuleFor(t => t.AdminEmail).Cascade(CascadeMode.Stop)
            .NotEmpty()
            .EmailAddress();
    }
}