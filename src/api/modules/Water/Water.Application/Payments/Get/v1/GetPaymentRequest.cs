using MediatR;

namespace FSH.Starter.WebApi.Water.Application.Payments.Get.v1;

public class GetPaymentRequest : IRequest<PaymentResponse>
{
    public Guid Id { get; set; }
    public GetPaymentRequest(Guid id) => Id = id;
}
