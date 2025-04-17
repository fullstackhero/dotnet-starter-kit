using FSH.Framework.Auditing.Contracts.Dtos;
using FSH.Framework.Auditing.Core.Entities;
using System.Collections.ObjectModel;

namespace FSH.Modules.Auditing.Core.Mappings;

public static class TrailMappings
{
    public static TrailDto ToDto(this Trail trail)
    {
        return new TrailDto
        {
            Id = trail.Id,
            DateTime = trail.DateTime,
            UserId = trail.UserId,
            Operation = trail.Operation,
            Description = trail.Description,
            EntityName = trail.EntityName ?? string.Empty,
            KeyValues = new Dictionary<string, object?>(trail.KeyValues),
            OldValues = new Dictionary<string, object?>(trail.OldValues),
            NewValues = new Dictionary<string, object?>(trail.NewValues),
            ModifiedProperties = new Collection<string>(trail.ModifiedProperties.ToList())
        };
    }


    public static IReadOnlyList<TrailDto> ToDtoList(this IEnumerable<Trail> trails)
    {
        return trails.Select(t => t.ToDto()).ToList();
    }

    public static Trail ToEntity(this TrailDto dto)
    {
        var entity = new Trail
        {
            Id = dto.Id,
            DateTime = dto.DateTime,
            UserId = dto.UserId,
            Operation = dto.Operation,
            Description = dto.Description,
            EntityName = dto.EntityName
        };

        entity.SetKeyValues(dto.KeyValues);
        entity.SetOldValues(dto.OldValues);
        entity.SetNewValues(dto.NewValues);
        entity.SetModifiedProperties(dto.ModifiedProperties);

        return entity;
    }

    public static IReadOnlyList<Trail> ToEntityList(this IEnumerable<TrailDto> dtos)
    {
        return dtos.Select(dto => dto.ToEntity()).ToList();
    }
}