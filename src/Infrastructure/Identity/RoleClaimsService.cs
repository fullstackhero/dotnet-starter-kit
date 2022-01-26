using FSH.WebApi.Application.Common.Caching;
using FSH.WebApi.Application.Common.Exceptions;
using FSH.WebApi.Application.Identity.RoleClaims;
using FSH.WebApi.Application.Identity.Roles;
using FSH.WebApi.Infrastructure.Persistence.Context;
using FSH.WebApi.Shared.Authorization;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace FSH.WebApi.Infrastructure.Identity;

public class RoleClaimsService : IRoleClaimsService
{
    private readonly ApplicationDbContext _db;
    private readonly ICacheService _cache;
    private readonly ICacheKeyService _cacheKeys;
    private readonly IStringLocalizer<RoleClaimsService> _localizer;

    public RoleClaimsService(ApplicationDbContext context, ICacheService cache, ICacheKeyService cacheKeys, IStringLocalizer<RoleClaimsService> localizer)
    {
        _db = context;
        _cache = cache;
        _cacheKeys = cacheKeys;
        _localizer = localizer;
    }

    public async Task<bool> HasPermissionAsync(string userId, string permission, CancellationToken cancellationToken)
    {
        var roles = await _cache.GetOrSetAsync(
            _cacheKeys.GetCacheKey(FSHClaims.Permission, userId),
            async () =>
            {
                var userRoles = await _db.UserRoles.Where(a => a.UserId == userId).Select(a => a.RoleId).ToListAsync();
                var applicationRoles = await _db.Roles.Where(a => userRoles.Contains(a.Id)).ToListAsync();
                return applicationRoles.Adapt<List<RoleDto>>();
            },
            cancellationToken: cancellationToken);

        if (roles is not null)
        {
            foreach (var role in roles)
            {
                if (await _db.RoleClaims.AnyAsync(
                    c => c.ClaimType == FSHClaims.Permission &&
                         c.ClaimValue == permission &&
                         c.RoleId == role.Id,
                    cancellationToken))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public async Task<List<RoleClaimDto>> GetAllAsync(CancellationToken cancellationToken) =>
        (await _db.RoleClaims.ToListAsync(cancellationToken))
            .Adapt<List<RoleClaimDto>>();

    public Task<int> GetCountAsync(CancellationToken cancellationToken) =>
        _db.RoleClaims.CountAsync(cancellationToken);

    public async Task<RoleClaimDto> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var roleClaim = await _db.RoleClaims
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        _ = roleClaim ?? throw new NotFoundException(_localizer["RoleClaim Not Found"]);

        return roleClaim.Adapt<RoleClaimDto>();
    }

    public async Task<List<RoleClaimDto>> GetAllByRoleIdAsync(string roleId, CancellationToken cancellationToken)
    {
        var roleClaims = await _db.RoleClaims
            .Where(x => x.RoleId == roleId)
            .ToListAsync(cancellationToken);

        return roleClaims.Adapt<List<RoleClaimDto>>();
    }

    public async Task<string> SaveAsync(RoleClaimRequest request, CancellationToken cancellationToken)
    {
        if (request.Id == 0)
        {
            var existingRoleClaim =
                await _db.RoleClaims
                    .SingleOrDefaultAsync(x => x.RoleId == request.RoleId && x.ClaimType == request.Type && x.ClaimValue == request.Value, cancellationToken: cancellationToken);
            if (existingRoleClaim is not null)
            {
                throw new ConflictException(_localizer["Similar Role Claim already exists."]);
            }

            var roleClaim = request.Adapt<ApplicationRoleClaim>();
            await _db.RoleClaims.AddAsync(roleClaim, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            return string.Format(_localizer["Role Claim {0} created."], request.Value);
        }
        else
        {
            var existingRoleClaim =
                await _db.RoleClaims
                    .SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (existingRoleClaim is null)
            {
                throw new NotFoundException(_localizer["RoleClaim Not Found"]);
            }

            existingRoleClaim.ClaimType = request.Type;
            existingRoleClaim.ClaimValue = request.Value;
            existingRoleClaim.Group = request.Group;
            existingRoleClaim.Description = request.Description;
            existingRoleClaim.RoleId = request.RoleId;
            _db.RoleClaims.Update(existingRoleClaim);
            await _db.SaveChangesAsync(cancellationToken);

            return string.Format(_localizer["Role Claim {0} for Role updated."], request.Value);
        }
    }

    public async Task<string> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var existingRoleClaim = await _db.RoleClaims
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (existingRoleClaim is null)
        {
            throw new NotFoundException(_localizer["RoleClaim Not Found"]);
        }

        _db.RoleClaims.Remove(existingRoleClaim);
        await _db.SaveChangesAsync(cancellationToken);

        return string.Format(_localizer["Role Claim {0} for Role deleted."], existingRoleClaim.ClaimValue);
    }
}