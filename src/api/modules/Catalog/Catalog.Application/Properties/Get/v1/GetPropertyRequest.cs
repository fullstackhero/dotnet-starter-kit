using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Properties.Get.v1;

public sealed record GetPropertyRequest(Guid Id) : IRequest<PropertyResponse>;