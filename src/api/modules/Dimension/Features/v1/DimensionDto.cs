namespace FSH.Starter.WebApi.Setting.Dimension.Features.v1;
public record DimensionDto(
    Guid Id,
    int? Order,    
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    string? FullName,
    string? NativeName,
    string? FullNativeName,
    int? Value,
    string Type,
    Guid? FatherId);
