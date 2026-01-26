using System.Linq.Expressions;
using FSH.Framework.Core.Context;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.v1.Users.SearchUsers;
using FSH.Modules.Identity.Data;
using FSH.Modules.Identity.Domain;
using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Identity.Features.v1.Users.SearchUsers;

public sealed class SearchUsersQueryHandler : IQueryHandler<SearchUsersQuery, PagedResponse<UserDto>>
{
    private readonly UserManager<FshUser> _userManager;
    private readonly IdentityDbContext _dbContext;
    private readonly IRequestContext _requestContext;

    public SearchUsersQueryHandler(
        UserManager<FshUser> userManager,
        IdentityDbContext dbContext,
        IRequestContext requestContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _requestContext = requestContext;
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
            ImageUrl = u.ImageUrl != null ? u.ImageUrl.ToString() : null
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
            ImageUrl = ResolveImageUrl(u.ImageUrl)
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

    private static readonly Dictionary<string, Expression<Func<FshUser, object?>>> SortableFields = new(StringComparer.OrdinalIgnoreCase)
    {
        ["firstname"] = u => u.FirstName,
        ["lastname"] = u => u.LastName,
        ["email"] = u => u.Email,
        ["username"] = u => u.UserName,
        ["isactive"] = u => u.IsActive
    };

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
            var (field, descending) = ParseSortField(part);
            
            if (!SortableFields.TryGetValue(field, out var selector))
            {
                selector = u => u.FirstName; // Default fallback
            }

            orderedQuery = ApplySortExpression(query, orderedQuery, selector, descending);
        }

        return orderedQuery ?? query.OrderBy(u => u.FirstName);
    }

    private static (string field, bool descending) ParseSortField(string part)
    {
        var descending = part.StartsWith('-');
        var field = descending ? part[1..] : part;
        return (field, descending);
    }

    private static IOrderedQueryable<FshUser> ApplySortExpression(
        IQueryable<FshUser> query,
        IOrderedQueryable<FshUser>? orderedQuery,
        Expression<Func<FshUser, object?>> selector,
        bool descending)
    {
        if (orderedQuery is null)
        {
            return descending 
                ? query.OrderByDescending(selector) 
                : query.OrderBy(selector);
        }

        return descending 
            ? orderedQuery.ThenByDescending(selector) 
            : orderedQuery.ThenBy(selector);
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

        var origin = _requestContext.Origin;
        if (string.IsNullOrEmpty(origin))
        {
            return imageUrl;
        }

        var relativePath = imageUrl.TrimStart('/');
        return $"{origin}/{relativePath}";
    }
}
