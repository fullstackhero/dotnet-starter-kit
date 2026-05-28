using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Billing.Data;
using FSH.Modules.Billing.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Billing.Features.v1.Invoices.GetInvoicePdf;

public sealed class GetInvoicePdfQueryHandler(
    BillingDbContext dbContext,
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor,
    IInvoicePdfRenderer renderer)
    : IQueryHandler<GetInvoicePdfQuery, InvoicePdfResult>
{
    public async ValueTask<InvoicePdfResult> Handle(GetInvoicePdfQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        // BillingDbContext is not tenant-filtered, so scope the fetch to the caller's tenant explicitly
        // (mirrors GetInvoiceById) — cross-tenant ids resolve to 404, never leak another tenant's PDF.
        var tenantId = tenantAccessor.MultiTenantContext?.TenantInfo?.Id
            ?? throw new UnauthorizedException("Tenant context is required.");

        var invoice = await dbContext.Invoices.AsNoTracking()
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == query.InvoiceId && i.TenantId == tenantId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Invoice {query.InvoiceId} not found.");

        var dto = invoice.ToDto();
        var content = renderer.Render(dto);
        return new InvoicePdfResult(content, $"{dto.InvoiceNumber}.pdf");
    }
}
