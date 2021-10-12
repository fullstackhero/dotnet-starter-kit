using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DN.WebApi.Application.Abstractions.Services.Identity;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Infrastructure.Identity.Models;
using DN.WebApi.Infrastructure.Persistence;
using DN.WebApi.Shared.DTOs.Identity.Requests;
using DN.WebApi.Shared.DTOs.Identity.Responses;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace DN.WebApi.Infrastructure.Identity.Services
{
    public class RoleClaimService : IRoleClaimService
    {
        private readonly IStringLocalizer<RoleClaimService> _localizer;
        private readonly ICurrentUser _currentUserService;
        private readonly ApplicationDbContext _db;

        public RoleClaimService(
            IStringLocalizer<RoleClaimService> localizer,
            ICurrentUser currentUserService,
            ApplicationDbContext db)
        {
            _localizer = localizer;
            _currentUserService = currentUserService;
            _db = db;
        }

        public async Task<Result<List<RoleClaimResponse>>> GetAllAsync()
        {
            var roleClaims = await _db.RoleClaims.ToListAsync();
            var roleClaimsResponse = roleClaims.Adapt<List<RoleClaimResponse>>();
            return await Result<List<RoleClaimResponse>>.SuccessAsync(roleClaimsResponse);
        }

        public async Task<int> GetCountAsync()
        {
            var count = await _db.RoleClaims.CountAsync();
            return count;
        }

        public async Task<Result<RoleClaimResponse>> GetByIdAsync(int id)
        {
            var roleClaim = await _db.RoleClaims
                .SingleOrDefaultAsync(x => x.Id == id);
            var roleClaimResponse = roleClaim.Adapt<RoleClaimResponse>();
            return await Result<RoleClaimResponse>.SuccessAsync(roleClaimResponse);
        }

        public Task<Result<List<RoleClaimResponse>>> GetAllByRoleIdAsync(string roleId)
        {
            // var roleClaims = await _db.RoleClaims
            //     .Include(x => x.Role)
            //     .Where(x => x.RoleId == roleId)
            //     .ToListAsync();
            // var roleClaimsResponse = roleClaims.Adapt<List<RoleClaimResponse>>();
            // return await Result<List<RoleClaimResponse>>.SuccessAsync(roleClaimsResponse);

            return default;
        }

        public async Task<Result<string>> SaveAsync(RoleClaimRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RoleId))
            {
                return await Result<string>.FailAsync(_localizer["Role is required."]);
            }

            if (request.Id == 0)
            {
                var existingRoleClaim =
                    await _db.RoleClaims
                        .SingleOrDefaultAsync(x =>
                            x.RoleId == request.RoleId && x.ClaimType == request.Type && x.ClaimValue == request.Value);
                if (existingRoleClaim != null)
                {
                    return await Result<string>.FailAsync(_localizer["Similar Role Claim already exists."]);
                }

                var roleClaim = request.Adapt<ApplicationRoleClaim>();
                await _db.RoleClaims.AddAsync(roleClaim);
                await _db.SaveChangesAsync();
                return await Result<string>.SuccessAsync(string.Format(_localizer["Role Claim {0} created."], request.Value));
            }
            else
            {
                var existingRoleClaim =
                    await _db.RoleClaims
                        .Include(x => x.RoleId)
                        .SingleOrDefaultAsync(x => x.Id == request.Id);
                if (existingRoleClaim == null)
                {
                    return await Result<string>.SuccessAsync(_localizer["Role Claim does not exist."]);
                }
                else
                {
                    existingRoleClaim.ClaimType = request.Type;
                    existingRoleClaim.ClaimValue = request.Value;
                    existingRoleClaim.Description = request.Description;
                    existingRoleClaim.RoleId = request.RoleId;
                    _db.RoleClaims.Update(existingRoleClaim);
                    await _db.SaveChangesAsync();

                    // return await Result<string>.SuccessAsync(string.Format(_localizer["Role Claim {0} for Role {1} updated."], request.Value, existingRoleClaim.Role.Name));

                    return default;
                }
            }
        }

        public Task<Result<string>> DeleteAsync(int id)
        {
            // var existingRoleClaim = await _db.RoleClaims
            //     .Include(x => x.Role)
            //     .FirstOrDefaultAsync(x => x.Id == id);
            // if (existingRoleClaim != null)
            // {
            //     _db.RoleClaims.Remove(existingRoleClaim);
            //     await _db.SaveChangesAsync();
            //     return await Result<string>.SuccessAsync(string.Format(_localizer["Role Claim {0} for {1} Role deleted."], existingRoleClaim.ClaimValue, existingRoleClaim.Role.Name));
            // }
            // else
            // {
            //     return await Result<string>.FailAsync(_localizer["Role Claim does not exist."]);
            // }

            return default;
        }
    }
}