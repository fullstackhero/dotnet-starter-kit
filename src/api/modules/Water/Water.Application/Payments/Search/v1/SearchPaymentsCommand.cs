using FSH.Framework.Core.Paging;
using FSH.Starter.WebApi.Water.Application.Payments.Get.v1;
using FSH.Starter.WebApi.Water.Domain;
using MediatR;

namespace FSH.Starter.WebApi.Water.Application.Payments.Search.v1;

public class SearchPaymentsCommand : PaginationFilter, IRequest<PagedList<PaymentResponse>>
{
    public Guid? BillId { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public PaymentStatus? Status { get; set; }
}
