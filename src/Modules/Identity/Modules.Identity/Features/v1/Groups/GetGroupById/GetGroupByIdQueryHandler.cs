using FSH.Framework.Core.Exceptions;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.v1.Groups.GetGroupById;
using FSH.Modules.Identity.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Identity.Features.v1.Groups.GetGroupById;

public sealed class GetGroupByIdQueryHandler(IdentityDbContext dbContext) : IQueryHandler<GetGroupByIdQuery, GroupDto>
{
    public async ValueTask<GroupDto> Handle(GetGroupByIdQuery query, CancellationToken cancellationToken)
    {
        var group = await dbContext.Groups
            .AsNoTracking()
            .Include(g => g.GroupRoles)
            .FirstOrDefaultAsync(g => g.Id == query.Id, cancellationToken)
            ?? throw new NotFoundException($"Group with ID '{query.Id}' not found.");

        var memberCount = await dbContext.UserGroups
            .CountAsync(ug => ug.GroupId == group.Id, cancellationToken);

        var roleIds = group.GroupRoles.Select(gr => gr.RoleId).ToList();
        var roleNames = roleIds.Count > 0
            ? await dbContext.Roles
                .AsNoTracking()
                .Where(r => roleIds.Contains(r.Id))
                .Select(r => r.Name!)
                .ToListAsync(cancellationToken)
            : [];

        return new GroupDto
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            IsDefault = group.IsDefault,
            IsSystemGroup = group.IsSystemGroup,
            MemberCount = memberCount,
            RoleIds = roleIds.AsReadOnly(),
            RoleNames = roleNames.AsReadOnly(),
            CreatedAt = group.CreatedOnUtc
        };
    }
}