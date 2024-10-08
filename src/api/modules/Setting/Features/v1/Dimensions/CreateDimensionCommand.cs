using System.ComponentModel;
using MediatR;

namespace FSH.Starter.WebApi.Setting.Features.v1.Dimensions;
public record CreateDimensionCommand(
    [property: DefaultValue(0)] int Order,
    [property: DefaultValue("string.Empty")] string Code,
    [property: DefaultValue("string.Empty")] string Name,
    [property: DefaultValue(null)] string? Description,
    [property: DefaultValue(true)] bool IsActive,
    [property: DefaultValue(null)] string? FullName,
    [property: DefaultValue(null)] string? NativeName,
    [property: DefaultValue(null)] string? FullNativeName,
    [property: DefaultValue(0)] int? Value,
    [property: DefaultValue("string.Empty")] string Type,
    Guid? FatherId
    ) : IRequest<CreateDimensionResponse>;

