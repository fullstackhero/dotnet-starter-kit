namespace FSH.WebApi.Application.Identity.Roles;

public class UpdatePermissionsRequest
{
    public string RoleId { get; set; } = default!;
    public List<string> Permissions { get; set; } = default!;
}

public class UpdatePermissionsRequestValidator : CustomValidator<UpdatePermissionsRequest>
{
    public UpdatePermissionsRequestValidator()
    {
        RuleFor(r => r.RoleId)
            .NotEmpty();
        RuleFor(r => r.Permissions)
            .NotNull();
    }
}