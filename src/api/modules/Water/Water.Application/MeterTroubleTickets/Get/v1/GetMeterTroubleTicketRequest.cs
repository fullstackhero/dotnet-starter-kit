using MediatR;

namespace FSH.Starter.WebApi.Water.Application.MeterTroubleTickets.Get.v1;

public class GetMeterTroubleTicketRequest : IRequest<MeterTroubleTicketResponse>
{
    public Guid Id { get; set; }
    public GetMeterTroubleTicketRequest(Guid id) => Id = id;
}
