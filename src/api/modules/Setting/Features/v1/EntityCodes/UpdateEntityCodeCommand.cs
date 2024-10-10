using FSH.Starter.WebApi.Setting.Domain;
using MediatR;

namespace FSH.Starter.WebApi.Setting.Features.v1.EntityCodes;
public sealed record UpdateEntityCodeCommand(
    Guid Id,
    int? Order,    
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    string? Separator,
    int? Value,
    CodeType Type
    ) : IRequest<UpdateEntityCodeResponse>;



