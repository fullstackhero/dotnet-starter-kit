using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.PropertyTypes.Get.v1;

public class GetPropertyTypeRequest : IRequest<PropertyTypeResponse>
{
    public Guid Id { get; set; }
    public GetPropertyTypeRequest(Guid id) => Id = id;
}
