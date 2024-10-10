using System.ComponentModel;
using FSH.Starter.WebApi.Setting.Domain;
using MediatR;

namespace FSH.Starter.WebApi.Setting.Features.v1.EntityCodes;
public record CreateEntityCodeCommand(
    [property: DefaultValue(0)] int Order,
    [property: DefaultValue("string.Empty")] string Code,
    [property: DefaultValue("")] string Name,
    [property: DefaultValue(null)] string? Description,
    [property: DefaultValue(true)] bool? IsActive,

    [property: DefaultValue(null)] string? Separator,
    [property: DefaultValue(0)] int? Value,
    [property: DefaultValue(CodeType.MasterData)] CodeType Type
    ) : IRequest<CreateEntityCodeResponse>;

