using FSH.Framework.Shared.Persistence;
using FSH.Modules.Billing.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Billing.Contracts.v1.Invoices;

/// <summary>
/// Admin query — lists invoices across all tenants with optional filters. Tenant-scoped callers
/// should use <c>GetMyInvoicesQuery</c> instead to avoid leaking cross-tenant data.
/// </summary>
public sealed record GetInvoicesQuery(
    string? TenantId = null,
    InvoiceStatus? Status = null,
    int? PeriodYear = null,
    int? PeriodMonth = null,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<PagedResponse<InvoiceDto>>;
