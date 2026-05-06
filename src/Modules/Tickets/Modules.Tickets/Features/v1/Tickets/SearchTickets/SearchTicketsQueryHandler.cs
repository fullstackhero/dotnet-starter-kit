using FSH.Framework.Shared.Persistence;
using FSH.Modules.Tickets.Contracts.Dtos;
using FSH.Modules.Tickets.Contracts.v1.Tickets;
using FSH.Modules.Tickets.Data;
using FSH.Modules.Tickets.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Tickets.Features.v1.Tickets.SearchTickets;

public sealed class SearchTicketsQueryHandler(TicketsDbContext dbContext)
    : IQueryHandler<SearchTicketsQuery, PagedResponse<TicketDto>>
{
    public async ValueTask<PagedResponse<TicketDto>> Handle(SearchTicketsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 20 : query.PageSize;

        var q = dbContext.Tickets.AsNoTracking().AsQueryable();

        if (query.Status is { } status)
        {
            q = q.Where(t => t.Status == status);
        }
        if (query.Priority is { } priority)
        {
            q = q.Where(t => t.Priority == priority);
        }
        if (query.AssignedToUserId is { } assignee)
        {
            q = q.Where(t => t.AssignedToUserId == assignee);
        }
        if (query.ReporterUserId is { } reporter)
        {
            q = q.Where(t => t.ReporterUserId == reporter);
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search.Trim();
            q = q.Where(t =>
                EF.Functions.ILike(t.Title, $"%{term}%") ||
                EF.Functions.ILike(t.Number, $"%{term}%") ||
                (t.Description != null && EF.Functions.ILike(t.Description, $"%{term}%")));
        }

        q = ApplySort(q, query.SortBy, query.SortDir);

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);

        // Project with comment count via subquery so we don't have to
        // materialize the comments collection just to count it.
        var projected = await q
            .Skip((page - 1) * size)
            .Take(size)
            .Select(t => new
            {
                Ticket = t,
                CommentCount = dbContext.TicketComments.Count(c => c.TicketId == t.Id),
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<TicketDto>
        {
            Items = projected.Select(p => p.Ticket.ToDto(p.CommentCount)).ToList(),
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size)
        };
    }

    private static IQueryable<Ticket> ApplySort(IQueryable<Ticket> q, string? sortBy, string? sortDir)
    {
        bool desc = !string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);
        return (sortBy?.ToLowerInvariant()) switch
        {
            "title"    => desc ? q.OrderByDescending(t => t.Title)    : q.OrderBy(t => t.Title),
            "priority" => desc ? q.OrderByDescending(t => t.Priority) : q.OrderBy(t => t.Priority),
            "status"   => desc ? q.OrderByDescending(t => t.Status)   : q.OrderBy(t => t.Status),
            "number"   => desc ? q.OrderByDescending(t => t.Number)   : q.OrderBy(t => t.Number),
            _ => desc ? q.OrderByDescending(t => t.CreatedAtUtc) : q.OrderBy(t => t.CreatedAtUtc),
        };
    }
}
