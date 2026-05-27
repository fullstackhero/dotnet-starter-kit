using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Billing.Contracts.Dtos;
using FSH.Modules.Billing.Contracts.v1.Invoices;
using FSH.Modules.Billing.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Billing.Features.v1.Invoices.GetInvoiceById;

public sealed class GetInvoiceByIdQueryHandler(
    BillingDbContext dbContext,
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor)
    : IQueryHandler<GetInvoiceByIdQuery, InvoiceDto>
{
    public async ValueTask<InvoiceDto> Handle(GetInvoiceByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        // BillingDbContext is not tenant-filtered (it extends raw DbContext for cross-tenant admin
        // visibility), so fetch-by-id MUST scope to the caller's tenant explicitly — otherwise any
        // tenant could read another tenant's invoice by guessing its id.
        var tenantId = tenantAccessor.MultiTenantContext?.TenantInfo?.Id
            ?? throw new UnauthorizedException("Tenant context is required.");

        var invoice = await dbContext.Invoices.AsNoTracking()
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == query.InvoiceId && i.TenantId == tenantId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Invoice {query.InvoiceId} not found.");

        return invoice.ToDto();
    }
}
