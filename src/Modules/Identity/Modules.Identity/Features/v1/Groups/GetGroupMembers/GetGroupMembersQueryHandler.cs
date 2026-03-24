using FSH.Framework.Core.Exceptions;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.v1.Groups.GetGroupMembers;
using FSH.Modules.Identity.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Identity.Features.v1.Groups.GetGroupMembers;

public sealed class GetGroupMembersQueryHandler : IQueryHandler<GetGroupMembersQuery, IEnumerable<GroupMemberDto>>
{
    private readonly IdentityDbContext _dbContext;

    public GetGroupMembersQueryHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<IEnumerable<GroupMemberDto>> Handle(GetGroupMembersQuery query, CancellationToken cancellationToken)
    {
        // Validate group exists
        var groupExists = await _dbContext.Groups
            .AnyAsync(g => g.Id == query.GroupId, cancellationToken);

        if (!groupExists)
        {
            throw new NotFoundException($"Group with ID '{query.GroupId}' not found.");
        }

        // Get memberships with user info
        var memberships = await _dbContext.UserGroups
            .AsNoTracking()
            .Where(ug => ug.GroupId == query.GroupId)
            .Join(
                _dbContext.Users,
                ug => ug.UserId,
                u => u.Id,
                (ug, u) => new GroupMemberDto
                {
                    UserId = u.Id,
                    UserName = u.UserName,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    AddedAt = ug.AddedAt,
                    AddedBy = ug.AddedBy
                })
            .OrderBy(m => m.UserName)
            .ToListAsync(cancellationToken);

        return memberships;
    }
}
