using System.ComponentModel.DataAnnotations;

namespace FSH.Starter.WebApi.Contracts.Auth;

internal sealed class TestMernisRequest
{
    [Required]
    public string Tckn { get; init; } = default!;

    [Required]
    public string FirstName { get; init; } = default!;

    [Required]
    public string LastName { get; init; } = default!;

    [Required]
    public int BirthYear { get; init; }
}
