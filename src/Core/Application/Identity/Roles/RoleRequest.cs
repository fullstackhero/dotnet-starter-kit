namespace FSH.WebApi.Application.Identity.Roles;

public class RoleRequest
{
    public string? Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
}

public class RoleRequestValidator : CustomValidator<RoleRequest>
{
    public RoleRequestValidator(IRoleService roleService, IStringLocalizer<RoleRequestValidator> localizer)
    {
        RuleFor(r => r.Name)
            .NotEmpty()
            .MustAsync(async (role, name, _) => !await roleService.ExistsAsync(name, role.Id))
                .WithMessage(localizer["Similar Role already exists."]);
    }
}