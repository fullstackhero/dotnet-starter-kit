using FSH.Framework.Shared.Persistence;
using FSH.Modules.Billing.Contracts.Dtos;
using FSH.Modules.Billing.Contracts.v1.Invoices;
using FSH.Modules.Billing.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Billing.Features.v1.Invoices.GetInvoices;

public sealed class GetInvoicesQueryHandler(BillingDbContext dbContext)
    : IQueryHandler<GetInvoicesQuery, PagedResponse<InvoiceDto>>
{
    public async ValueTask<PagedResponse<InvoiceDto>> Handle(GetInvoicesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var q = dbContext.Invoices.AsNoTracking().Include(i => i.LineItems).AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.TenantId))
        {
            q = q.Where(i => i.TenantId == query.TenantId);
        }
        if (query.Status is not null)
        {
            q = q.Where(i => i.Status == query.Status);
        }
        if (query.PeriodYear is not null)
        {
            q = q.Where(i => i.PeriodYear == query.PeriodYear);
        }
        if (query.PeriodMonth is not null)
        {
            q = q.Where(i => i.PeriodMonth == query.PeriodMonth);
        }

        var total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);
        var invoices = await q
            .OrderByDescending(i => i.CreatedAtUtc)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return new PagedResponse<InvoiceDto>
        {
            Items = invoices.Select(i => i.ToDto()).ToList(),
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)query.PageSize)
        };
    }
}
