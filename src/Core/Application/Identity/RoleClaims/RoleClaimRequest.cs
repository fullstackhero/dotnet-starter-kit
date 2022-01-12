namespace FSH.WebApi.Application.Identity.RoleClaims;

public class RoleClaimRequest
{
    public int Id { get; set; }
    public string RoleId { get; set; } = default!;
    public string? Type { get; set; }
    public string? Value { get; set; }
    public string? Description { get; set; }
    public string? Group { get; set; }
    public bool Selected { get; set; }
}

public class RoleClaimRequestValidator : CustomValidator<RoleClaimRequest>
{
    public RoleClaimRequestValidator() =>
        RuleFor(r => r.RoleId)
            .NotEmpty();
}