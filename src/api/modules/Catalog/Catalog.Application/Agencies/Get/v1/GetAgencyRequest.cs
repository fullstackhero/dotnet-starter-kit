using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Agencies.Get.v1;
public class GetAgencyRequest : IRequest<AgencyResponse>
{
    public Guid Id { get; set; }
    public GetAgencyRequest(Guid id) => Id = id;
}
