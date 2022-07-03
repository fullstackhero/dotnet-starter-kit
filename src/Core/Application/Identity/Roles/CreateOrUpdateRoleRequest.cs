namespace FSH.WebApi.Application.Identity.Roles;

public class CreateOrUpdateRoleRequest : IRequest<string>
{
    public string? Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
}

public class CreateOrUpdateRoleRequestValidator : AbstractValidator<CreateOrUpdateRoleRequest>
{
    public CreateOrUpdateRoleRequestValidator(IRoleService roleService, IStringLocalizer<CreateOrUpdateRoleRequestValidator> T) =>
        RuleFor(r => r.Name)
            .NotEmpty()
            .MustAsync(async (role, name, _) => !await roleService.ExistsAsync(name, role.Id))
                .WithMessage(T["Similar Role already exists."]);
}

public class CreateOrUpdateRoleRequestHandler : IRequestHandler<CreateOrUpdateRoleRequest, string>
{
    private readonly IRoleService _roleService;
    public CreateOrUpdateRoleRequestHandler(IRoleService roleService) => _roleService = roleService;

    public Task<string> Handle(CreateOrUpdateRoleRequest req, CancellationToken ct) =>
        _roleService.CreateOrUpdateAsync(req, ct);
}