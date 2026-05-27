namespace FSH.Modules.Identity.Contracts.DTOs;

public class GroupMemberDto
{
    public string UserId { get; set; } = default!;
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime AddedAt { get; set; }
    public string? AddedBy { get; set; }
}