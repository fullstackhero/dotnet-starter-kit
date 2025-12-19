using FSH.Framework.Persistence;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.v1.Users.SearchUsers;
using FSH.Modules.Identity.Data;
using FSH.Modules.Identity.Features.v1.Users;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using FSH.Framework.Web.Origin;

namespace FSH.Modules.Identity.Features.v1.Users.SearchUsers;

public sealed class SearchUsersQueryHandler : IQueryHandler<SearchUsersQuery, PagedResponse<UserDto>>
{
    private readonly UserManager<FshUser> _userManager;
    private readonly IdentityDbContext _dbContext;
    private readonly Uri? _originUrl;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SearchUsersQueryHandler(
        UserManager<FshUser> userManager,
        IdentityDbContext dbContext,
        IOptions<OriginOptions> originOptions,
        IHttpContextAccessor httpContextAccessor)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _originUrl = originOptions.Value.OriginUrl;
        _httpContextAccessor = httpContextAccessor;
    }

    public async ValueTask<PagedResponse<UserDto>> Handle(SearchUsersQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        IQueryable<FshUser> users = _userManager.Users.AsNoTracking();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search.ToLowerInvariant();
            users = users.Where(u =>
                (u.FirstName != null && u.FirstName.ToLower().Contains(term)) ||
                (u.LastName != null && u.LastName.ToLower().Contains(term)) ||
                (u.Email != null && u.Email.ToLower().Contains(term)) ||
                (u.UserName != null && u.UserName.ToLower().Contains(term)));
        }

        if (query.IsActive.HasValue)
        {
            users = users.Where(u => u.IsActive == query.IsActive.Value);
        }

        if (query.EmailConfirmed.HasValue)
        {
            users = users.Where(u => u.EmailConfirmed == query.EmailConfirmed.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.RoleId))
        {
            var userIdsInRole = await _dbContext.UserRoles
                .Where(ur => ur.RoleId == query.RoleId)
                .Select(ur => ur.UserId)
                .ToListAsync(cancellationToken);

            users = users.Where(u => userIdsInRole.Contains(u.Id));
        }

        // Apply sorting
        users = ApplySorting(users, query.Sort);

        // Project to DTO
        var projected = users.Select(u => new UserDto
        {
            Id = u.Id,
            UserName = u.UserName,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Email = u.Email,
            IsActive = u.IsActive,
            EmailConfirmed = u.EmailConfirmed,
            PhoneNumber = u.PhoneNumber,
            ImageUrl = u.ImageUrl != null ? u.ImageUrl.ToString() : null,
            LastPasswordChangeUtc = u.LastPasswordChangeUtc
        });

        var pagedResult = await projected.ToPagedResponseAsync(query, cancellationToken).ConfigureAwait(false);

        // Resolve image URLs for items
        var items = pagedResult.Items.Select(u => new UserDto
        {
            Id = u.Id,
            UserName = u.UserName,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Email = u.Email,
            IsActive = u.IsActive,
            EmailConfirmed = u.EmailConfirmed,
            PhoneNumber = u.PhoneNumber,
            ImageUrl = ResolveImageUrl(u.ImageUrl),
            LastPasswordChangeUtc = u.LastPasswordChangeUtc
        }).ToList();

        return new PagedResponse<UserDto>
        {
            Items = items,
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize,
            TotalCount = pagedResult.TotalCount,
            TotalPages = pagedResult.TotalPages
        };
    }

    private static IQueryable<FshUser> ApplySorting(IQueryable<FshUser> query, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return query.OrderBy(u => u.FirstName).ThenBy(u => u.LastName);
        }

        var sortParts = sort.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        IOrderedQueryable<FshUser>? orderedQuery = null;

        foreach (var part in sortParts)
        {
            var descending = part.StartsWith('-');
            var field = descending ? part[1..] : part;

            orderedQuery = (orderedQuery, field.ToLowerInvariant()) switch
            {
                (null, "firstname") => descending ? query.OrderByDescending(u => u.FirstName) : query.OrderBy(u => u.FirstName),
                (null, "lastname") => descending ? query.OrderByDescending(u => u.LastName) : query.OrderBy(u => u.LastName),
                (null, "email") => descending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
                (null, "username") => descending ? query.OrderByDescending(u => u.UserName) : query.OrderBy(u => u.UserName),
                (null, "isactive") => descending ? query.OrderByDescending(u => u.IsActive) : query.OrderBy(u => u.IsActive),
                (null, _) => query.OrderBy(u => u.FirstName),

                (not null, "firstname") => descending ? orderedQuery.ThenByDescending(u => u.FirstName) : orderedQuery.ThenBy(u => u.FirstName),
                (not null, "lastname") => descending ? orderedQuery.ThenByDescending(u => u.LastName) : orderedQuery.ThenBy(u => u.LastName),
                (not null, "email") => descending ? orderedQuery.ThenByDescending(u => u.Email) : orderedQuery.ThenBy(u => u.Email),
                (not null, "username") => descending ? orderedQuery.ThenByDescending(u => u.UserName) : orderedQuery.ThenBy(u => u.UserName),
                (not null, "isactive") => descending ? orderedQuery.ThenByDescending(u => u.IsActive) : orderedQuery.ThenBy(u => u.IsActive),
                (not null, _) => orderedQuery.ThenBy(u => u.FirstName)
            };
        }

        return orderedQuery ?? query.OrderBy(u => u.FirstName);
    }

    private string? ResolveImageUrl(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return null;
        }

        if (Uri.TryCreate(imageUrl, UriKind.Absolute, out _))
        {
            return imageUrl;
        }

        if (_originUrl is null)
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request is not null && !string.IsNullOrWhiteSpace(request.Scheme) && request.Host.HasValue)
            {
                var baseUri = $"{request.Scheme}://{request.Host.Value}{request.PathBase}";
                var relativePath = imageUrl.TrimStart('/');
                return $"{baseUri.TrimEnd('/')}/{relativePath}";
            }

            return imageUrl;
        }

        var originRelativePath = imageUrl.TrimStart('/');
        return $"{_originUrl.AbsoluteUri.TrimEnd('/')}/{originRelativePath}";
    }
}
