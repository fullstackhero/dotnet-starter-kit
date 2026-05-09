using Ardalis.Specification;
using FSH.Framework.Core.Paging;
using FSH.Framework.Core.Specifications;
using FSH.Starter.WebApi.Water.Application.Payments.Get.v1;
using FSH.Starter.WebApi.Water.Domain;

namespace FSH.Starter.WebApi.Water.Application.Payments.Search.v1;

public class SearchPaymentSpecs : EntitiesByPaginationFilterSpec<Payment, PaymentResponse>
{
    public SearchPaymentSpecs(SearchPaymentsCommand command)
        : base(command) =>
        Query
            .OrderByDescending(c => c.PaymentDate, !command.HasOrderBy())
            .Where(p => p.BillId == command.BillId, command.BillId.HasValue)
            .Where(p => p.PaymentMethod == command.PaymentMethod, command.PaymentMethod.HasValue)
            .Where(p => p.Status == command.Status, command.Status.HasValue);
}
