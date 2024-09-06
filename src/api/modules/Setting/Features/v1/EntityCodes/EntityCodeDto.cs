namespace FSH.Starter.WebApi.Setting.Features.v1.EntityCodes;
public record EntityCodeDto(
    Guid Id,
    int? Order,    
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    string? Separator,
    int? Value,
    string Type);
