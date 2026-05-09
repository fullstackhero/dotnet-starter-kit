using MediatR;

namespace FSH.Starter.WebApi.Water.Application.Bills.Get.v1;

public class GetBillRequest : IRequest<BillResponse>
{
    public Guid Id { get; set; }
    public GetBillRequest(Guid id) => Id = id;
}
