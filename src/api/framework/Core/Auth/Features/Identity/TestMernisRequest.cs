using MediatR;

namespace FSH.Framework.Core.Auth.Features.Identity;

public record TestMernisRequest : IRequest<TestMernisResult>
{
    public string Tckn { get; init; } = default!;
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public int BirthYear { get; init; }
}

public record TestMernisResult
{
    public bool IsValid { get; init; }
    public string Message { get; init; } = default!;
} 