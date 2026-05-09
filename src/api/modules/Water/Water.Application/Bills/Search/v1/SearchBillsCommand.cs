using FSH.Framework.Core.Paging;
using FSH.Starter.WebApi.Water.Application.Bills.Get.v1;
using FSH.Starter.WebApi.Water.Domain;
using MediatR;

namespace FSH.Starter.WebApi.Water.Application.Bills.Search.v1;

public class SearchBillsCommand : PaginationFilter, IRequest<PagedList<BillResponse>>
{
    public Guid? CustomerId { get; set; }
    public BillStatus? Status { get; set; }
    public int? BillingMonth { get; set; }
    public int? BillingYear { get; set; }
}
