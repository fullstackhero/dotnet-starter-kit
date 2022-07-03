namespace FSH.WebApi.Application.Identity.Roles;

public class UpdateRolePermissionsRequest : IRequest<string>
{
    public string RoleId { get; set; } = default!;
    public List<string> Permissions { get; set; } = default!;
}

public class UpdateRolePermissionsRequestValidator : AbstractValidator<UpdateRolePermissionsRequest>
{
    public UpdateRolePermissionsRequestValidator()
    {
        RuleFor(r => r.RoleId)
            .NotEmpty();
        RuleFor(r => r.Permissions)
            .NotNull();
    }
}

public class UpdateRolePermissionsRequestHandler : IRequestHandler<UpdateRolePermissionsRequest, string>
{
    private readonly IRoleService _roleService;
    public UpdateRolePermissionsRequestHandler(IRoleService roleService) => _roleService = roleService;

    public Task<string> Handle(UpdateRolePermissionsRequest req, CancellationToken ct) =>
        _roleService.UpdatePermissionsAsync(req, ct);
}