using FSH.Framework.Core.Exceptions;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.v1.Groups.GetGroupById;
using FSH.Modules.Identity.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Identity.Features.v1.Groups.GetGroupById;

public sealed class GetGroupByIdQueryHandler : IQueryHandler<GetGroupByIdQuery, GroupDto>
{
    private readonly IdentityDbContext _dbContext;

    public GetGroupByIdQueryHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<GroupDto> Handle(GetGroupByIdQuery query, CancellationToken cancellationToken)
    {
        var result = await _dbContext.Groups
            .Where(g => g.Id == query.Id)
            .Select(g => new GroupDto
            {
                Id = g.Id,
                Name = g.Name,
                Description = g.Description,
                IsDefault = g.IsDefault,
                IsSystemGroup = g.IsSystemGroup,
                MemberCount = g.UserGroups.Count,
                RoleIds = g.GroupRoles.Select(gr => gr.RoleId).ToList().AsReadOnly(),
                RoleNames = g.GroupRoles.Select(gr => gr.Role!.Name!).ToList().AsReadOnly(),
                CreatedOnUtc = g.CreatedOnUtc
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Group with ID '{query.Id}' not found.");

        return result;
    }
}
